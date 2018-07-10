using Microsoft.Extensions.Logging;
using System;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class WatchdogFactory : IWatchdogFactory
	{
		/// <summary>
		/// The <see cref="IChat"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		readonly IChat chat;

		/// <summary>
		/// The <see cref="ISessionControllerFactory"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		readonly ISessionControllerFactory sessionManagerFactory;

		/// <summary>
		/// The <see cref="IServerUpdater"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		readonly IServerUpdater serverUpdater;

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// The <see cref="IReattachInfoHandler"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		readonly IReattachInfoHandler reattachInfoHandler;

		/// <summary>
		/// Construct a <see cref="WatchdogFactory"/>
		/// </summary>
		/// <param name="chat">The value of <see cref="chat"/></param>
		/// <param name="sessionManagerFactory">The value of <see cref="sessionManagerFactory"/></param>
		/// <param name="serverUpdater">The value of <see cref="serverUpdater"/></param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/></param>
		/// <param name="reattachInfoHandler">The value of <see cref="reattachInfoHandler"/></param>
		public WatchdogFactory(IChat chat, ISessionControllerFactory sessionManagerFactory, IServerUpdater serverUpdater, ILoggerFactory loggerFactory, IReattachInfoHandler reattachInfoHandler)
		{
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			this.sessionManagerFactory = sessionManagerFactory ?? throw new ArgumentNullException(nameof(sessionManagerFactory));
			this.serverUpdater = serverUpdater ?? throw new ArgumentNullException(nameof(serverUpdater));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.reattachInfoHandler = reattachInfoHandler ?? throw new ArgumentNullException(nameof(reattachInfoHandler));
		}

		/// <inheritdoc />
		public IWatchdog CreateWatchdog(IDmbFactory dmbFactory, DreamDaemonLaunchParameters launchParameters) => new Watchdog(chat, sessionManagerFactory, dmbFactory, serverUpdater, loggerFactory.CreateLogger<Watchdog>(), reattachInfoHandler, launchParameters);
	}
}
