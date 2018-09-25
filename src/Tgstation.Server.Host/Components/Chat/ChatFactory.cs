using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Tgstation.Server.Host.Components.Chat.Commands;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <inheritdoc />
	sealed class ChatFactory : IChatFactory
	{
		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="ChatFactory"/>
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// The <see cref="IProviderFactory"/> for the <see cref="ChatFactory"/>
		/// </summary>
		readonly IProviderFactory providerFactory;

		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="ChatFactory"/>
		/// </summary>
		readonly IServerControl serverControl;

		/// <summary>
		/// Construct a <see cref="ChatFactory"/>
		/// </summary>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/></param>
		/// <param name="providerFactory">The value of <see cref="providerFactory"/></param>
		/// <param name="serverControl">The value of <see cref="serverControl"/></param>
		public ChatFactory(ILoggerFactory loggerFactory, IProviderFactory providerFactory, IServerControl serverControl)
		{
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
		}

		/// <inheritdoc />
		public IChat CreateChat(IIOManager ioManager, ICommandFactory commandFactory, IEnumerable<Models.ChatBot> initialChatBots) => new Chat(providerFactory, ioManager, commandFactory, serverControl, loggerFactory.CreateLogger<Chat>(), initialChatBots);
	}
}
