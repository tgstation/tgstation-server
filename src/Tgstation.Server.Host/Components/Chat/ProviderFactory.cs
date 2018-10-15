using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Chat.Providers;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <inheritdoc />
	sealed class ProviderFactory : IProviderFactory
	{
		/// <summary>
		/// The <see cref="IApplication"/> for the <see cref="ProviderFactory"/>
		/// </summary>
		readonly IApplication application;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="ProviderFactory"/>
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="ProviderFactory"/>
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// Construct a <see cref="ProviderFactory"/>
		/// </summary>
		/// <param name="application">The value of <see cref="application"/></param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/></param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/></param>
		public ProviderFactory(IApplication application, IAsyncDelayer asyncDelayer, ILoggerFactory loggerFactory)
		{
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.application = application ?? throw new ArgumentNullException(nameof(application));
		}

		/// <inheritdoc />
		public IProvider CreateProvider(Api.Models.Internal.ChatBot settings)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));
			var builder = settings.ConnectionStringBuilder;
			if (builder == null || !builder.Valid)
				throw new InvalidOperationException("Invalid ChatConnectionStringBuilder!");
			switch (settings.Provider)
			{
				case ChatProvider.Irc:
					var ircBuilder = (IrcConnectionStringBuilder)builder;
					return new IrcProvider(application, asyncDelayer, loggerFactory.CreateLogger<IrcProvider>(), ircBuilder.Address, ircBuilder.Port.Value, ircBuilder.Nickname, ircBuilder.Password, ircBuilder.PasswordType, ircBuilder.UseSsl.Value);
				case ChatProvider.Discord:
					var discordBuilder = (DiscordConnectionStringBuilder)builder;
					return new DiscordProvider(loggerFactory.CreateLogger<DiscordProvider>(), discordBuilder.BotToken);
				default:
					throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid ChatProvider: {0}", settings.Provider));
			}
		}
	}
}
