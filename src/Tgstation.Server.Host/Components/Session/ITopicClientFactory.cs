using System;

using Byond.TopicSender;

#nullable disable

namespace Tgstation.Server.Host.Components.Session
{
	/// <summary>
	/// Factory for <see cref="ITopicClient"/>s.
	/// </summary>
	interface ITopicClientFactory
	{
		/// <summary>
		/// Create a <see cref="ITopicClient"/>.
		/// </summary>
		/// <param name="timeout">The request timeout.</param>
		/// <returns>A new <see cref="ITopicClient"/>.</returns>
		ITopicClient CreateTopicClient(TimeSpan timeout);
	}
}
