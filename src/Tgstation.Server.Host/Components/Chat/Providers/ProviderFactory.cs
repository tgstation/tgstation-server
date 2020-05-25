using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Chat.Providers
{
	/// <inheritdoc />
	sealed class ProviderFactory : IProviderFactory
	{
		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="ProviderFactory"/>
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

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
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/></param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/></param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/></param>
		public ProviderFactory(
			IAssemblyInformationProvider assemblyInformationProvider,
			IAsyncDelayer asyncDelayer,
			ILoggerFactory loggerFactory)
		{
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
		}

		/// <inheritdoc />
		public IProvider CreateProvider(Api.Models.Internal.ChatBot settings)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));
			var builder = settings.CreateConnectionStringBuilder();
			if (builder == null || !builder.Valid)
				throw new InvalidOperationException("Invalid ChatConnectionStringBuilder!");
			switch (settings.Provider)
			{
				case ChatProvider.Irc:
					var ircBuilder = (IrcConnectionStringBuilder)builder;
					return new IrcProvider(assemblyInformationProvider, asyncDelayer, loggerFactory.CreateLogger<IrcProvider>(), ircBuilder.Address, ircBuilder.Port.Value, ircBuilder.Nickname, ircBuilder.Password, ircBuilder.PasswordType, settings.ReconnectionInterval.Value, ircBuilder.UseSsl.Value);
				case ChatProvider.Discord:
					var discordBuilder = (DiscordConnectionStringBuilder)builder;
					return new DiscordProvider(
						assemblyInformationProvider,
						loggerFactory.CreateLogger<DiscordProvider>(),
						discordBuilder.BotToken,
						settings.ReconnectionInterval.Value);
				default:
					throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid ChatProvider: {0}", settings.Provider));
			}
		}
	}
}
