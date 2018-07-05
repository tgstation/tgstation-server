using System;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class WatchdogFactory : IWatchdogFactory
	{
		/// <summary>
		/// The <see cref="IByond"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		readonly IByond byond;

		/// <summary>
		/// The <see cref="IChat"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		readonly IChat chat;

		/// <summary>
		/// The <see cref="ISessionControllerFactory"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		readonly ISessionControllerFactory sessionManagerFactory;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="IInteropRegistrar"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		readonly IInteropRegistrar interopRegistrar;

		/// <summary>
		/// The <see cref="IServerUpdater"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		readonly IServerUpdater serverUpdater;

		/// <summary>
		/// Construct a <see cref="WatchdogFactory"/>
		/// </summary>
		/// <param name="byond">The value of <see cref="byond"/></param>
		/// <param name="chat">The value of <see cref="chat"/></param>
		/// <param name="sessionManagerFactory">The value of <see cref="sessionManagerFactory"/></param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		/// <param name="interopRegistrar">The value of <see cref="interopRegistrar"/></param>
		/// <param name="serverUpdater">The value of <see cref="serverUpdater"/></param>
		public WatchdogFactory(IChat chat, ISessionControllerFactory sessionManagerFactory, IEventConsumer eventConsumer, IInteropRegistrar interopRegistrar, IServerUpdater serverUpdater)
		{
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			this.sessionManagerFactory = sessionManagerFactory ?? throw new ArgumentNullException(nameof(sessionManagerFactory));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.interopRegistrar = interopRegistrar ?? throw new ArgumentNullException(nameof(interopRegistrar));
			this.serverUpdater = serverUpdater ?? throw new ArgumentNullException(nameof(serverUpdater));
		}

		/// <inheritdoc />
		public IWatchdog CreateWatchdog(IDmbFactory dmbFactory, DreamDaemonLaunchParameters launchParameters) => new Watchdog(chat, sessionManagerFactory, dmbFactory, eventConsumer, interopRegistrar, serverUpdater, launchParameters);
	}
}
