using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// The watchdog for a <see cref="Host"/>
	/// </summary>
	public interface IWatchdog
	{
		/// <summary>
		/// Run the <see cref="IWatchdog"/>
		/// </summary>
		/// <param name="args">The arguments for the <see cref="IWatchdog"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task RunAsync(string[] args, CancellationToken cancellationToken);
	}
}
