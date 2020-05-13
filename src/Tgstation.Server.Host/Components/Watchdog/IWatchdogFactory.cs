using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// For creating <see cref="IWatchdog"/>s
	/// </summary>
	interface IWatchdogFactory
	{
		/// <summary>
		/// Creates a <see cref="IWatchdog"/>
		/// </summary>
		/// <param name="chat">The <see cref="IChatManager"/> for the <see cref="IWatchdog"/></param>
		/// <param name="dmbFactory">The <see cref="IDmbFactory"/> for the <see cref="IWatchdog"/> with</param>
		/// <param name="reattachInfoHandler">The <see cref="IReattachInfoHandler"/> for the <see cref="IWatchdog"/></param>
		/// <param name="sessionControllerFactory">The <see cref="ISessionControllerFactory"/> for the <see cref="IWatchdog"/></param>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="IWatchdog"/>.</param>
		/// <param name="instance">The <see cref="Instance"/> for the <see cref="IWatchdog"/></param>
		/// <param name="settings">The initial <see cref="DreamDaemonSettings"/> for the <see cref="IWatchdog"/></param>
		/// <returns>A new <see cref="IWatchdog"/></returns>
		IWatchdog CreateWatchdog(
			IChatManager chat,
			IDmbFactory dmbFactory,
			IReattachInfoHandler reattachInfoHandler,
			ISessionControllerFactory sessionControllerFactory,
			IIOManager ioManager,
			Api.Models.Instance instance,
			DreamDaemonSettings settings);
	}
}
