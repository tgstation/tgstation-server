using System;

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
		/// The <see cref="ISessionManagerFactory"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		readonly ISessionManagerFactory sessionManagerFactory;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="IInteropRegistrar"/> for the <see cref="WatchdogFactory"/>
		/// </summary>
		readonly IInteropRegistrar interopRegistrar;

		/// <summary>
		/// Construct a <see cref="WatchdogFactory"/>
		/// </summary>
		/// <param name="byond">The value of <see cref="byond"/></param>
		/// <param name="chat">The value of <see cref="chat"/></param>
		/// <param name="sessionManagerFactory">The value of <see cref="sessionManagerFactory"/></param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		/// <param name="interopRegistrar">The value of <see cref="interopRegistrar"/></param>
		public WatchdogFactory(IByond byond, IChat chat, ISessionManagerFactory sessionManagerFactory, IEventConsumer eventConsumer, IInteropRegistrar interopRegistrar)
		{
			this.byond = byond ?? throw new ArgumentNullException(nameof(byond));
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			this.sessionManagerFactory = sessionManagerFactory ?? throw new ArgumentNullException(nameof(sessionManagerFactory));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.interopRegistrar = interopRegistrar ?? throw new ArgumentNullException(nameof(interopRegistrar));
		}

		/// <inheritdoc />
		public IWatchdog CreateWatchdog(IDmbFactory dmbFactory) => new Watchdog(byond, chat, sessionManagerFactory, dmbFactory, eventConsumer, interopRegistrar);
	}
}
