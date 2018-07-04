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
		/// <param name="compileJob">The <see cref="IDmbFactory"/> for the <see cref="IWatchdog"/> with</param>
		/// <returns>A new <see cref="IWatchdog"/></returns>
		IWatchdog CreateWatchdog(IDmbFactory dmbFactory);
	}
}
