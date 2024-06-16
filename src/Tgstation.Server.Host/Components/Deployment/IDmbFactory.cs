using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// Factory for <see cref="IDmbProvider"/>s.
	/// </summary>
	public interface IDmbFactory : ILatestCompileJobProvider, IComponentService, IDisposable
	{
		/// <summary>
		/// Get a <see cref="Task"/> that completes when the result of a call to <see cref="LockNextDmb"/> will be different than the previous call if any.
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task OnNewerDmb { get; }

		/// <summary>
		/// If <see cref="LockNextDmb"/> will succeed.
		/// </summary>
		bool DmbAvailable { get; }

		/// <summary>
		/// Gets the next <see cref="IDmbProvider"/>. <see cref="DmbAvailable"/> is a precondition.
		/// </summary>
		/// <param name="reason">The reason the lock is being acquired.</param>
		/// <param name="callerFile">The file path of the calling function.</param>
		/// <param name="callerLine">The line number of the call invocation.</param>
		/// <returns>A new <see cref="IDmbProvider"/>.</returns>
		IDmbProvider LockNextDmb(string reason, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = default);

		/// <summary>
		/// Gets a <see cref="IDmbProvider"/> for a given <see cref="CompileJob"/>.
		/// </summary>
		/// <param name="compileJob">The <see cref="CompileJob"/> to make the <see cref="IDmbProvider"/> for.</param>
		/// <param name="reason">The reason the compile job needed to be loaded.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <param name="callerFile">The file path of the calling function.</param>
		/// <param name="callerLine">The line number of the call invocation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="IDmbProvider"/> representing the <see cref="CompileJob"/> on success, <see langword="null"/> on failure.</returns>
		ValueTask<IDmbProvider?> FromCompileJob(CompileJob compileJob, string reason, CancellationToken cancellationToken, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = default);

		/// <summary>
		/// Deletes all compile jobs that are inactive in the Game folder.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask CleanUnusedCompileJobs(CancellationToken cancellationToken);
	}
}
