using System;

using Byond.TopicSender;
using Microsoft.Extensions.Logging;

namespace Tgstation.Server.Host.Components.Session
{
	/// <inheritdoc />
	sealed class TopicClientFactory : ITopicClientFactory
	{
		/// <summary>
		/// The <see cref="ILogger"/> for created <see cref="ITopicClient"/>s.
		/// </summary>
		readonly ILogger<TopicClient> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="TopicClientFactory"/> class.
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public TopicClientFactory(ILogger<TopicClient> logger)
		{
			ArgumentNullException.ThrowIfNull(logger);

			// Don't want the debug logs Topic client spits out either, they're too verbose
			if (logger.IsEnabled(LogLevel.Trace))
				this.logger = logger;
		}

		/// <inheritdoc />
		public ITopicClient CreateTopicClient(TimeSpan timeout)
			=> new TopicClient(
				new SocketParameters
				{
					ConnectTimeout = timeout,
					DisconnectTimeout = timeout,
					ReceiveTimeout = timeout,
					SendTimeout = timeout,
				},
				logger);
	}
}
