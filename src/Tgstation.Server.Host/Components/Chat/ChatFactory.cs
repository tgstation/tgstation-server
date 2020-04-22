using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Tgstation.Server.Host.Components.Chat.Commands;
using Tgstation.Server.Host.Components.Chat.Providers;
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
		/// The <see cref="IAsyncDelayer"/> for the <see cref="ChatFactory"/>
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// Construct a <see cref="ChatFactory"/>
		/// </summary>
		/// <param name="providerFactory">The value of <see cref="providerFactory"/></param>
		/// <param name="serverControl">The value of <see cref="serverControl"/></param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/></param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/></param>
		public ChatFactory(IProviderFactory providerFactory, IServerControl serverControl, IAsyncDelayer asyncDelayer, ILoggerFactory loggerFactory)
		{
			this.providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
		}

		/// <inheritdoc />
		public IChat CreateChat(IIOManager ioManager, ICommandFactory commandFactory, IEnumerable<Models.ChatBot> initialChatBots) => new Chat(providerFactory, ioManager, commandFactory, serverControl, asyncDelayer, loggerFactory, loggerFactory.CreateLogger<Chat>(), initialChatBots);
	}
}
