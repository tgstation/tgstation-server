using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For monitoring DreamDaemon uptime
	/// </summary>
    interface IWatchdog
    {
		/// <summary>
		/// Start the <see cref="IWatchdog"/>
		/// </summary>
		/// <param name="launchParametersFactory">The <see cref="ILaunchParametersFactory"/> for the run</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Start(ILaunchParametersFactory launchParametersFactory, CancellationToken cancellationToken);

		/// <summary>
		/// Stop the <see cref="IWatchdog"/>
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Stop();
	}
}
