using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// Represents process lifetime.
	/// </summary>
	public interface IProcessBase
	{
		/// <summary>
		/// The <see cref="Task{TResult}"/> resulting in the exit code of the process.
		/// </summary>
		Task<int> Lifetime { get; }

		/// <summary>
		/// Set's the owned <see cref="global::System.Diagnostics.Process.PriorityClass"/> to <see cref="global::System.Diagnostics.ProcessPriorityClass.AboveNormal"/>.
		/// </summary>
		void SetHighPriority();

		/// <summary>
		/// Suspends the process.
		/// </summary>
		void Suspend();

		/// <summary>
		/// Resumes the process.
		/// </summary>
		void Unsuspend();

		/// <summary>
		/// Create a dump file of the process.
		/// </summary>
		/// <param name="outputFile">The full path to the output file.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task CreateDump(string outputFile, CancellationToken cancellationToken);
	}
}
