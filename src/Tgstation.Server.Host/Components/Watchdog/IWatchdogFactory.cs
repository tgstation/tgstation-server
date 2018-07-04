using Tgstation.Server.Api.Models.Internal;

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
		/// <param name="dmbFactory">The <see cref="IDmbFactory"/> for the <see cref="IWatchdog"/> with</param>
		/// <param name="launchParameters">The initial <see cref="DreamDaemonLaunchParameters"/> for the <see cref="IWatchdog"/></param>
		/// <returns>A new <see cref="IWatchdog"/></returns>
		IWatchdog CreateWatchdog(IDmbFactory dmbFactory, DreamDaemonLaunchParameters launchParameters);
	}
}
