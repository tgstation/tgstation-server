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
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
