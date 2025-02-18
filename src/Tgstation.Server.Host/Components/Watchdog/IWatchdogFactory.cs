using Prometheus;

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Deployment.Remote;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// For creating <see cref="IWatchdog"/>s.
	/// </summary>
	interface IWatchdogFactory
	{
		/// <summary>
		/// Creates a <see cref="IWatchdog"/>.
		/// </summary>
		/// <param name="chat">The <see cref="IChatManager"/> for the <see cref="IWatchdog"/>.</param>
		/// <param name="dmbFactory">The <see cref="IDmbFactory"/> for the <see cref="IWatchdog"/> with.</param>
		/// <param name="sessionPersistor">The <see cref="ISessionPersistor"/> for the <see cref="IWatchdog"/>.</param>
		/// <param name="sessionControllerFactory">The <see cref="ISessionControllerFactory"/> for the <see cref="IWatchdog"/>.</param>
		/// <param name="gameIOManager">The <see cref="IIOManager"/> pointing to the Game directory for the <see cref="IWatchdog"/>.</param>
		/// <param name="diagnosticsIOManager">The <see cref="IIOManager"/> pointing to the Diagnostics directory for the <see cref="IWatchdog"/>.</param>
		/// <param name="eventConsumer">The <see cref="IEventConsumer"/> for the <see cref="IWatchdog"/>.</param>
		/// <param name="remoteDeploymentManagerFactory">The <see cref="IRemoteDeploymentManagerFactory"/> for the <see cref="IWatchdog"/>.</param>
		/// <param name="metricFactory">The <see cref="IMetricFactory"/> used to create metrics.</param>
		/// <param name="instance">The <see cref="Instance"/> for the <see cref="IWatchdog"/>.</param>
		/// <param name="settings">The initial <see cref="DreamDaemonSettings"/> for the <see cref="IWatchdog"/>.</param>
		/// <returns>A new <see cref="IWatchdog"/>.</returns>
		IWatchdog CreateWatchdog(
			IChatManager chat,
			IDmbFactory dmbFactory,
			ISessionPersistor sessionPersistor,
			ISessionControllerFactory sessionControllerFactory,
			IIOManager gameIOManager,
			IIOManager diagnosticsIOManager,
			IEventConsumer eventConsumer,
			IRemoteDeploymentManagerFactory remoteDeploymentManagerFactory,
			IMetricFactory metricFactory,
			Api.Models.Instance instance,
			DreamDaemonSettings settings);
	}
}
