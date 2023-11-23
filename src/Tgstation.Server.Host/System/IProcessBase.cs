using System.Threading;
using System.Threading.Tasks;

#nullable disable

namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// Represents process lifetime.
	/// </summary>
	interface IProcessBase
	{
		/// <summary>
		/// The <see cref="Task{TResult}"/> resulting in the exit code of the process or <see langword="null"/> if the process was detached.
		/// </summary>
		Task<int?> Lifetime { get; }

		/// <summary>
		/// Set's the owned <see cref="global::System.Diagnostics.Process.PriorityClass"/> to a non-normal value.
		/// </summary>
		/// <param name="higher">If <see langword="true"/> will be set to <see cref="global::System.Diagnostics.ProcessPriorityClass.AboveNormal"/> otherwise, will be set to <see cref="global::System.Diagnostics.ProcessPriorityClass.BelowNormal"/>.</param>
		void AdjustPriority(bool higher);

		/// <summary>
		/// Suspends the process.
		/// </summary>
		void Suspend();

		/// <summary>
		/// Resumes the process.
		/// </summary>
		void Resume();

		/// <summary>
		/// Create a dump file of the process.
		/// </summary>
		/// <param name="outputFile">The full path to the output file.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask CreateDump(string outputFile, CancellationToken cancellationToken);
	}
}
