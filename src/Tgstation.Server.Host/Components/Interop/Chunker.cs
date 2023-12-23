using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Class that deserializes chunked interop payloads.
	/// </summary>
	abstract class Chunker
	{
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Chunker"/>.
		/// </summary>
		protected ILogger<Chunker> Logger { get; }

		/// <summary>
		/// Gets a payload ID for use in a new <see cref="ChunkSetInfo"/>.
		/// </summary>
		protected uint NextPayloadId
		{
			get
			{
				// 0 is special, since BYOND doesn't use it we reserve it for if we have to send a chunked topic request immediately upon reattaching
				// Otherwise, jump ahead a bunch compared to what was last used/seen, so we don't accidentally clash
				lock (chunkSets)
					return highestSeenPayloadId == 0 ? 0 : highestSeenPayloadId + 20;
			}
		}

		/// <summary>
		/// The cache of chunked communications.
		/// </summary>
		/// <remarks>If the DMAPI is erroring, this can present a memory leak. Worth expiring entries if they aren't completed after some minutes.</remarks>
		readonly Dictionary<uint, Tuple<ChunkSetInfo, string[]>> chunkSets;

		/// <summary>
		/// The highest payload ID value seen.
		/// </summary>
		uint highestSeenPayloadId;

		/// <summary>
		/// Initializes a new instance of the <see cref="Chunker"/> class.
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		protected Chunker(ILogger<Chunker> logger)
		{
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			chunkSets = new Dictionary<uint, Tuple<ChunkSetInfo, string[]>>();
		}

		/// <summary>
		/// Process a given <paramref name="chunk"/>.
		/// </summary>
		/// <typeparam name="TCommunication">The <see cref="Type"/> of communication that was chunked.</typeparam>
		/// <typeparam name="TResponse">The <see cref="Type"/> of <see cref="IMissingPayloadsCommunication"/> expected.</typeparam>
		/// <param name="completionCallback">The callback that receives the completed <typeparamref name="TCommunication"/>.</param>
		/// <param name="chunkErrorCallback">The callback that generates a <typeparamref name="TResponse"/> for a given error.</param>
		/// <param name="chunk">The <see cref="ChunkData"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <typeparamref name="TResponse"/> for the chunked request.</returns>
		protected async ValueTask<TResponse> ProcessChunk<TCommunication, TResponse>(
			Func<TCommunication?, CancellationToken, ValueTask<TResponse>> completionCallback,
			Func<string, TResponse> chunkErrorCallback,
			ChunkData chunk,
			CancellationToken cancellationToken)
			where TResponse : IMissingPayloadsCommunication, new()
		{
			if (chunk == null)
				return chunkErrorCallback("Missing chunk!");

			if (!chunk.PayloadId.HasValue)
				return chunkErrorCallback("Missing chunk payloadId!");

			if (!chunk.SequenceId.HasValue)
				return chunkErrorCallback("Missing chunk sequenceId!");

			if (chunk.Payload == null)
				return chunkErrorCallback("Missing chunk payload!");

			ChunkSetInfo requestInfo;
			string[] payloads;
			lock (chunkSets)
			{
				highestSeenPayloadId = Math.Max(chunk.PayloadId.Value, highestSeenPayloadId);
				if (!chunkSets.TryGetValue(chunk.PayloadId.Value, out var tuple))
				{
					// first time seeing this payload
					if (chunk.TotalChunks == 0)
						return chunkErrorCallback("Receieved chunked request with 0 totalChunks!");

					tuple = Tuple.Create<ChunkSetInfo, string[]>(chunk, new string[chunk.TotalChunks]);
					chunkSets.Add(chunk.PayloadId.Value, tuple);
				}

				requestInfo = tuple.Item1;
				payloads = tuple.Item2;

				Logger.LogTrace("Received chunk P{payloadId}: {sequenceId}/{totalChunks}", requestInfo.PayloadId, chunk.SequenceId + 1, requestInfo.TotalChunks);

				if (chunk.TotalChunks != requestInfo.TotalChunks)
				{
					chunkSets.Remove(requestInfo.PayloadId!.Value);
					return chunkErrorCallback("Received differing total chunks for same payloadId! Invalidating payloadId!");
				}

				if (payloads[chunk.SequenceId.Value] != null && payloads[chunk.SequenceId.Value] != chunk.Payload)
				{
					chunkSets.Remove(requestInfo.PayloadId!.Value);
					return chunkErrorCallback("Received differing payload for same sequenceId! Invalidating payloadId!");
				}

				payloads[chunk.SequenceId.Value] = chunk.Payload;
				var missingPayloads = new List<uint>();
				for (var i = 0U; i < payloads.Length; ++i)
					if (payloads[i] == null)
						missingPayloads.Add(i);

				if (missingPayloads.Count > 0)
					return new TResponse
					{
						MissingChunks = missingPayloads,
					};

				Logger.LogTrace("Received all chunks for P{payloadId}, processing request...", requestInfo.PayloadId);
				chunkSets.Remove(requestInfo.PayloadId!.Value);
			}

			TCommunication? completedCommunication;
			var fullCommunicationJson = String.Concat(payloads);
			try
			{
				completedCommunication = JsonConvert.DeserializeObject<TCommunication>(fullCommunicationJson, DMApiConstants.SerializerSettings);
			}
			catch (Exception ex)
			{
				Logger.LogDebug(ex, "Bad chunked communication for payload {payloadId}!", requestInfo.PayloadId);
				return chunkErrorCallback("Chunked request completed with bad JSON!");
			}

			return await completionCallback(completedCommunication, cancellationToken);
		}
	}
}
