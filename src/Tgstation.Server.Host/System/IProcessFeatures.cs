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
		/// <returns>The name of the user executing <paramref name="process"/>.</returns>
		string GetExecutingUsername(global::System.Diagnostics.Process process);

		/// <summary>
		/// Suspend a given <paramref name="process"/>.
		/// </summary>
		/// <param name="process">The <see cref="Process"/> to suspend.</param>
		void SuspendProcess(global::System.Diagnostics.Process process);

		/// <summary>
		/// Resume a given suspended <see cref="Process"/>.
		/// </summary>
		/// <param name="process">The <see cref="Process"/> to suspended.</param>
		void ResumeProcess(global::System.Diagnostics.Process process);

		/// <summary>
		/// Create a dump file for a given <paramref name="process"/>.
		/// </summary>
		/// <param name="process">The <see cref="Process"/> to dump.</param>
		/// <param name="outputFile">The full path to the output file.</param>
		/// <param name="minidump">If a minidump should be taken as opposed to a full dump.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask CreateDump(global::System.Diagnostics.Process process, string outputFile, bool minidump, CancellationToken cancellationToken);
	}
}
