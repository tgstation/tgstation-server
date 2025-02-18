using System;
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
		/// When the process was started.
		/// </summary>
		DateTimeOffset? LaunchTime { get; }

		/// <summary>
		/// The <see cref="Task{TResult}"/> resulting in the exit code of the process or <see langword="null"/> if the process was detached.
		/// </summary>
		Task<int?> Lifetime { get; }

		/// <summary>
		/// Gets the process' memory usage in bytes.
		/// </summary>
		long? MemoryUsage { get; }

		/// <summary>
		/// Gets the estimated CPU usage fraction of the process based on the last time this was called.
		/// </summary>
		/// <returns>The CPU's estimated usage as a value between 0 and 1.</returns>
		double MeasureProcessorTimeDelta();

		/// <summary>
		/// Set's the owned <see cref="global::System.Diagnostics.Process.PriorityClass"/> to a non-normal value.
		/// </summary>
		/// <param name="higher">If <see langword="true"/> will be set to <see cref="global::System.Diagnostics.ProcessPriorityClass.AboveNormal"/> otherwise, will be set to <see cref="global::System.Diagnostics.ProcessPriorityClass.BelowNormal"/>.</param>
		void AdjustPriority(bool higher);

		/// <summary>
		/// Suspends the process.
		/// </summary>
		void SuspendProcess();

		/// <summary>
		/// Resumes the process.
		/// </summary>
		void ResumeProcess();

		/// <summary>
		/// Create a dump file of the process.
		/// </summary>
		/// <param name="outputFile">The full path to the output file.</param>
		/// <param name="minidump">If a minidump should be taken as opposed to a full dump.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask CreateDump(string outputFile, bool minidump, CancellationToken cancellationToken);
	}
}
