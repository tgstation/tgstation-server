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
		/// The <see cref="ILoggerFactory"/> for the <see cref="ProviderFactory"/>
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// The <see cref="IApplication"/> for the <see cref="ProviderFactory"/>
		/// </summary>
		readonly IApplication application;

		/// <summary>
		/// Construct a <see cref="ProviderFactory"/>
		/// </summary>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/></param>
		/// <param name="application">The value of <see cref="application"/></param>
		public ProviderFactory(ILoggerFactory loggerFactory, IApplication application)
		{
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.application = application ?? throw new ArgumentNullException(nameof(application));
		}

		/// <inheritdoc />
		public IProvider CreateProvider(Api.Models.Internal.ChatBot settings)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));
			switch (settings.Provider)
			{
				case ChatProvider.Irc:
					//Connection string semicolon delimited until the password field
					if (settings.ConnectionString == null)
						throw new InvalidOperationException("ConnectionString cannot be null!");
					var splits = settings.ConnectionString.Split(';');
					if (splits.Length < 4)
						throw new InvalidOperationException("Invalid connection string!");

					var address = splits[0];
					if (!UInt16.TryParse(splits[1], out var port))
						throw new InvalidOperationException("Unable to parse port!");
					var nick = splits[2];
					if (!Int32.TryParse(splits[3], out var intSsl))
						throw new InvalidOperationException("Unable to parse ssl option!");

					IrcPasswordType? passwordType = null;
					string password = null;
					if(splits.Length > 4)
					{
						if (splits.Length < 6)
							throw new InvalidOperationException("Invalid connection string!");
						if (!Int32.TryParse(splits[4], out var intPasswordType))
							throw new InvalidOperationException("Unable to parse password type!");

						passwordType = (IrcPasswordType)intPasswordType;
						switch (passwordType)
						{
							case IrcPasswordType.NickServ:
							case IrcPasswordType.Sasl:
							case IrcPasswordType.Server:
								break;
							default:
								throw new InvalidOperationException("Invalid password type!");
						}

						var rest = new List<string>(splits);
						rest.RemoveRange(0, 5);
						password = String.Join(";", rest);
					}

					return new IrcProvider(loggerFactory.CreateLogger<IrcProvider>(), application, address, port, nick, password, passwordType, intSsl != 0);
				case ChatProvider.Discord:
					//discord is just the bot token
					return new DiscordProvider(loggerFactory.CreateLogger<DiscordProvider>(), settings.ConnectionString);
				default:
					throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid ChatProvider: {0}", settings.Provider));
			}
		}
	}
}
