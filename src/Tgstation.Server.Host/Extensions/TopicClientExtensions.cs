using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Byond.TopicSender;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for <see cref="ITopicClient"/>.
	/// </summary>
	static class TopicClientExtensions
	{
		/// <summary>
		/// Counter for topic request logging.
		/// </summary>
		static ulong topicRequestId;

		/// <summary>
		/// Send a <paramref name="queryString"/> with optional repeated priority.
		/// </summary>
		/// <param name="topicClient">The <see cref="ITopicClient"/> to send with.</param>
		/// <param name="delayer">The <see cref="IAsyncDelayer"/> to use for delayed retries if an error occurs.</param>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="queryString">The <see cref="string"/> to send.</param>
		/// <param name="port">The local port to send the topic to.</param>
		/// <param name="priority">If priority retries should be used.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="TopicResponse"/> on success, <see langword="null"/> on failure.</returns>
		public static async ValueTask<TopicResponse?> SendWithOptionalPriority(
			this ITopicClient topicClient,
			IAsyncDelayer delayer,
			ILogger logger,
			string queryString,
			ushort port,
			bool priority,
			CancellationToken cancellationToken)
		{
			const int PrioritySendAttempts = 5;
			var endpoint = new IPEndPoint(IPAddress.Loopback, port);
			var firstSend = true;

			for (var i = PrioritySendAttempts - 1; i >= 0 && (priority || firstSend); --i)
				try
				{
					firstSend = false;

					var localRequestId = Interlocked.Increment(ref topicRequestId);
					logger.LogTrace("Begin topic request #{requestId}: {query}", localRequestId, queryString);
					var byondResponse = await topicClient.SendTopic(
						endpoint,
						queryString,
						cancellationToken);

					logger.LogTrace("End topic request #{requestId}", localRequestId);
					return byondResponse;
				}
				catch (Exception ex) when (ex is not OperationCanceledException && ex is not ArgumentException)
				{
					logger.LogWarning(ex, "SendTopic exception!{retryDetails}", priority ? $" {i} attempts remaining." : String.Empty);

					if (priority && i > 0)
						await delayer.Delay(TimeSpan.FromSeconds(2), cancellationToken);
				}

			return null;
		}
	}
}
