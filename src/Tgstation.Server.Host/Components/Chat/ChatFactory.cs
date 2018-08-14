using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Tgstation.Server.Host.Components.Chat.Commands;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <inheritdoc />
	sealed class ChatFactory : IChatFactory
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="ChatFactory"/>
		/// </summary>
		readonly IIOManager ioManager;
		
		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="ChatFactory"/>
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// The <see cref="ICommandFactory"/> for the <see cref="ChatFactory"/>
		/// </summary>
		readonly ICommandFactory commandFactory;

		/// <summary>
		/// The <see cref="IProviderFactory"/> for the <see cref="ChatFactory"/>
		/// </summary>
		readonly IProviderFactory providerFactory;

		/// <summary>
		/// Construct a <see cref="ChatFactory"/>
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/></param>
		/// <param name="commandFactory">The value of <see cref="commandFactory"/></param>
		/// <param name="providerFactory">The value of <see cref="providerFactory"/></param>
		public ChatFactory(IIOManager ioManager, ILoggerFactory loggerFactory, ICommandFactory commandFactory, IProviderFactory providerFactory)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.commandFactory = commandFactory ?? throw new ArgumentNullException(nameof(commandFactory));
			this.providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
		}

		/// <inheritdoc />
		public IChat CreateChat(IEnumerable<Models.ChatBot> initialChatBots) => new Chat(providerFactory, ioManager, commandFactory, loggerFactory.CreateLogger<Chat>(), initialChatBots);
	}
}
