using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Components.Chat.Commands;
using Tgstation.Server.Host.Components.Chat.Providers;
using Tgstation.Server.Host.Core;

#nullable disable

namespace Tgstation.Server.Host.Components.Chat
{
	/// <inheritdoc />
	sealed class ChatManagerFactory : IChatManagerFactory
	{
		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="ChatManagerFactory"/>.
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// The <see cref="IProviderFactory"/> for the <see cref="ChatManagerFactory"/>.
		/// </summary>
		readonly IProviderFactory providerFactory;

		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="ChatManagerFactory"/>.
		/// </summary>
		readonly IServerControl serverControl;

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatManagerFactory"/> class.
		/// </summary>
		/// <param name="providerFactory">The value of <see cref="providerFactory"/>.</param>
		/// <param name="serverControl">The value of <see cref="serverControl"/>.</param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/>.</param>
		public ChatManagerFactory(
			IProviderFactory providerFactory,
			IServerControl serverControl,
			ILoggerFactory loggerFactory)
		{
			this.providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
		}

		/// <inheritdoc />
		public IChatManager CreateChatManager(
			ICommandFactory commandFactory,
			IEnumerable<Models.ChatBot> initialChatBots)
			=> new ChatManager(
				providerFactory,
				commandFactory,
				serverControl,
				loggerFactory,
				loggerFactory.CreateLogger<ChatManager>(),
				initialChatBots.Where(x => x.Enabled.Value));
	}
}
