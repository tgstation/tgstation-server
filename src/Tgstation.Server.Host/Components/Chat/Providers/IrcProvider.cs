using Meebey.SmartIrc4net;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Chat.Providers
{
	/// <summary>
	/// <see cref="IProvider"/> for internet relay chat
	/// </summary>
	sealed class IrcProvider : IProvider
	{
		/// <inheritdoc />
		public bool Connected => throw new NotImplementedException();

		/// <inheritdoc />
		public string BotMention => throw new NotImplementedException();

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="IrcProvider"/>
		/// </summary>
		readonly ILogger<IrcProvider> logger;

		/// <summary>
		/// The <see cref="IrcFeatures"/> client
		/// </summary>
		readonly IrcFeatures client;

		/// <summary>
		/// Address of the server to connect to
		/// </summary>
		readonly string address;
		/// <summary>
		/// Port of the server to connect to
		/// </summary>
		readonly ushort port;
		/// <summary>
		/// IRC nickname
		/// </summary>
		readonly string nickname;
		/// <summary>
		/// Password which will used for authentication
		/// </summary>
		readonly string password;
		/// <summary>
		/// The <see cref="IrcPasswordType"/> of <see cref="password"/>
		/// </summary>
		readonly IrcPasswordType? passwordType;

		/// <summary>
		/// Construct an <see cref="IrcProvider"/>
		/// </summary>
		/// <param name="logger">The value of logger</param>
		/// <param name="application">The <see cref="IApplication"/> to get the <see cref="IApplication.VersionString"/> from</param>
		/// <param name="address">The value of <see cref="address"/></param>
		/// <param name="port">The value of <see cref="port"/></param>
		/// <param name="nickname">The value of <see cref="nickname"/></param>
		/// <param name="password">The value of <see cref="password"/></param>
		/// <param name="passwordType">The value of <see cref="passwordType"/></param>
		/// <param name="useSsl">If <see cref="IrcConnection.UseSsl"/> should be used</param>
		public IrcProvider(ILogger<IrcProvider> logger, IApplication application, string address, ushort port, string nickname, string password, IrcPasswordType? passwordType, bool useSsl)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			if (application == null)
				throw new ArgumentNullException(nameof(application));

			this.address = address ?? throw new ArgumentNullException(nameof(address));
			this.port = port;
			this.nickname = nickname ?? throw new ArgumentNullException(nameof(nickname));

			if (passwordType.HasValue && password == null)
				throw new ArgumentNullException(nameof(password));

			if(password != null && !passwordType.HasValue)
				throw new ArgumentNullException(nameof(passwordType));

			this.password = password;
			this.passwordType = passwordType;

			client = new IrcFeatures
			{
				SupportNonRfc = true,
				CtcpUserInfo = "You are going to play. And I am going to watch. And everything will be just fine...",
				AutoRejoin = true,
				AutoRejoinOnKick = true,
				AutoRelogin = true,
				AutoRetry = true,
				AutoRetryLimit = 5,
				AutoRetryDelay = 5,
				ActiveChannelSyncing = true,
				AutoNickHandling = true,
				CtcpVersion = application.VersionString,
				UseSsl = useSsl,
				ValidateServerCertificate = useSsl,
			};

			client.OnChannelMessage += Client_OnChannelMessage;
			client.OnQueryMessage += Client_OnQueryMessage;
		}

		void Client_OnQueryMessage(object sender, IrcEventArgs e)
		{
			throw new NotImplementedException();
		}

		void Client_OnChannelMessage(object sender, IrcEventArgs e)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public void Dispose() => Disconnect(default).Wait();    //not actually a task so whatever

		/// <inheritdoc />
		public Task<bool> Connect(CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
		{
			try
			{
				client.Connect(address, port);

				cancellationToken.ThrowIfCancellationRequested();

				if (passwordType == IrcPasswordType.Sasl)
				{
					//TODO
				}

				if (passwordType == IrcPasswordType.Server)
					client.Login(nickname, nickname, 0, nickname, password);
				else
					client.Login(nickname, nickname);

				if (passwordType == IrcPasswordType.NickServ)
				{
					cancellationToken.ThrowIfCancellationRequested();
					client.SendMessage(SendType.Message, "NickServ", String.Format(CultureInfo.InvariantCulture, "IDENTIFY {0}", password));
				}
			}
			catch(Exception e)
			{
				logger.LogWarning("Unable to connect to IRC: {0}", e);
			}
			return true;
		}, cancellationToken,TaskCreationOptions.LongRunning, TaskScheduler.Current);

		/// <inheritdoc />
		public Task Disconnect(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<IReadOnlyList<Channel>> MapChannels(IEnumerable<ChatChannel> channels, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<Message> NextMessage(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task SendMessage(ulong channelId, string message, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
