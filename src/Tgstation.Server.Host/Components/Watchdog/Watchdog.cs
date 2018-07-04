using System;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class Watchdog : IWatchdog
	{
		public DreamDaemonLaunchParameters LaunchParameters
		{
			get => launchParameters;
			set => launchParameters = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// The <see cref="IByond"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IByond byond;

		/// <summary>
		/// The <see cref="IChat"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IChat chat;

		/// <summary>
		/// The <see cref="ISessionManagerFactory"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly ISessionManagerFactory sessionManagerFactory;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="IInteropRegistrar"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IInteropRegistrar interopRegistrar;

		/// <summary>
		/// The <see cref="IDmbFactory"/> for the <see cref="Watchdog"/>
		/// </summary>
		readonly IDmbFactory dmbFactory;

		DreamDaemonLaunchParameters launchParameters;

		ISessionManager alphaServer;
		ISessionManager bravoServer;

		/// <summary>
		/// Construct a <see cref="IWatchdog"/>
		/// </summary>
		/// <param name="byond">The value of <see cref="byond"/></param>
		/// <param name="chat">The value of <see cref="chat"/></param>
		/// <param name="sessionManagerFactory">The value of <see cref="sessionManagerFactory"/></param>
		/// <param name="dmbFactory">The value of <see cref="dmbFactory"/></param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/></param>
		/// <param name="interopRegistrar">The value of <see cref="interopRegistrar"/></param>
		public Watchdog(IByond byond, IChat chat, ISessionManagerFactory sessionManagerFactory, IDmbFactory dmbFactory, IEventConsumer eventConsumer, IInteropRegistrar interopRegistrar, DreamDaemonLaunchParameters initialLaunchParameters)
		{
			this.byond = byond ?? throw new ArgumentNullException(nameof(byond));
			this.chat = chat ?? throw new ArgumentNullException(nameof(chat));
			this.sessionManagerFactory = sessionManagerFactory ?? throw new ArgumentNullException(nameof(sessionManagerFactory));
			this.dmbFactory = dmbFactory ?? throw new ArgumentNullException(nameof(dmbFactory));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.interopRegistrar = interopRegistrar ?? throw new ArgumentNullException(nameof(interopRegistrar));
			LaunchParameters = initialLaunchParameters;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			alphaServer?.Dispose();
			bravoServer?.Dispose();
		}
	}
}
