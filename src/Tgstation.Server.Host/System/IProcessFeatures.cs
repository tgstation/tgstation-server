using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// Abstraction for suspending and resuming processes.
	/// </summary>
	interface IProcessFeatures
	{
		/// <summary>
		/// Get the name of the user executing a given <paramref name="process"/>.
		/// </summary>
		/// <param name="process">The <see cref="global::System.Diagnostics.Process"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The name of the user executing <paramref name="process"/>.</returns>
		Task<string> GetExecutingUsername(global::System.Diagnostics.Process process, CancellationToken cancellationToken);

		/// <summary>
		/// Suspend a given <see cref="Process"/>.
		/// </summary>
		/// <param name="process">The <see cref="Process"/> to suspend.</param>
		void SuspendProcess(global::System.Diagnostics.Process process);

		/// <summary>
		/// Resume a given suspended <see cref="Process"/>.
		/// </summary>
		/// <param name="process">The <see cref="Process"/> to susperesumend.</param>
		void ResumeProcess(global::System.Diagnostics.Process process);
	}
}
