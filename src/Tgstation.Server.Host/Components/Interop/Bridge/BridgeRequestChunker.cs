using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	/// <summary>
	/// Processes chunked bridge requests.
	/// </summary>
	abstract class BridgeRequestChunker : IBridgeDispatcher
	{
		/// <summary>
		/// The cache of chunked bridge requests.
		/// </summary>
		/// <remarks>If the DMAPI is erroring, this can present a memory leak. Worth expiring entries if they aren't completed after some minutes.</remarks>
		readonly Dictionary<uint, Tuple<ChunkedRequestInfo, string[]>> bridgeChunks;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="BridgeRequestChunker"/>.
		/// </summary>
		protected ILogger<BridgeRequestChunker> Logger { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="BridgeRequestChunker"/> class.
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		protected BridgeRequestChunker(ILogger<BridgeRequestChunker> logger)
		{
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			bridgeChunks = new Dictionary<uint, Tuple<ChunkedRequestInfo, string[]>>();
		}

		/// <inheritdoc />
		public abstract Task<BridgeResponse> ProcessBridgeRequest(BridgeParameters parameters, CancellationToken cancellationToken);

		/// <summary>
		/// Process a given <paramref name="chunk"/>.
		/// </summary>
		/// <param name="chunk">The <see cref="ChunkData"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="BridgeResponse"/> for the chunked request.</returns>
		protected async Task<BridgeResponse> ProcessBridgeChunk(ChunkData chunk, CancellationToken cancellationToken)
		{
			if (chunk == null)
				return BridgeError("Missing chunk!");

			ChunkedRequestInfo requestInfo;
			string[] payloads;
			lock (bridgeChunks)
			{
				if (!bridgeChunks.TryGetValue(chunk.PayloadId, out var tuple))
				{
					// first time seeing this payload
					if (chunk.TotalChunks == 0)
						return BridgeError("Receieved chunked request with 0 totalChunks!");

					tuple = Tuple.Create<ChunkedRequestInfo, string[]>(chunk, new string[chunk.TotalChunks]);
					bridgeChunks.Add(chunk.PayloadId, tuple);
				}

				requestInfo = tuple.Item1;
				payloads = tuple.Item2;

				Logger.LogTrace("Received bridge payload chunk P{payloadId}: {sequenceId}/{totalChunks}", requestInfo.PayloadId, chunk.SequenceId + 1, requestInfo.TotalChunks);

				if (chunk.TotalChunks != requestInfo.TotalChunks)
				{
					bridgeChunks.Remove(requestInfo.PayloadId);
					return BridgeError("Received differing total chunks for same payloadId! Invalidating payloadId!");
				}

				if (payloads[chunk.SequenceId] != null && payloads[chunk.SequenceId] != chunk.Payload)
				{
					bridgeChunks.Remove(requestInfo.PayloadId);
					return BridgeError("Received differing payload for same sequenceId! Invalidating payloadId!");
				}

				payloads[chunk.SequenceId] = chunk.Payload;
				var missingPayloads = new List<uint>();
				for (uint i = 0; i < payloads.Length; ++i)
					if (payloads[i] == null)
						missingPayloads.Add(i);

				if (missingPayloads.Count > 0)
					return new BridgeResponse
					{
						MissingChunks = missingPayloads,
					};

				Logger.LogTrace("Received all bridge chunks for P{payloadId}, processing request...", requestInfo.PayloadId);
				bridgeChunks.Remove(requestInfo.PayloadId);
			}

			BridgeParameters completedRequest;
			var fullRequestJson = String.Concat(payloads);
			try
			{
				completedRequest = JsonConvert.DeserializeObject<BridgeParameters>(fullRequestJson, DMApiConstants.SerializerSettings);
			}
			catch (Exception ex)
			{
				Logger.LogWarning(ex, "Bad chunked bridge request for payload {payloadId}!", requestInfo.PayloadId);
				return BridgeError("Chunked request completed with bad JSON!", false);
			}

			return await ProcessBridgeRequest(completedRequest, cancellationToken);
		}

		/// <summary>
		/// Create and logs an errored <see cref="BridgeResponse"/>.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="log">If <paramref name="message"/> should be written to the <see cref="Logger"/>.</param>
		/// <returns>A new errored <see cref="BridgeResponse"/>.</returns>
		protected BridgeResponse BridgeError(string message, bool log = true)
		{
			if (log)
				Logger.LogWarning("Bridge processing error: {errorMessage}", message);

			return new BridgeResponse
			{
				ErrorMessage = message,
			};
		}
	}
}
