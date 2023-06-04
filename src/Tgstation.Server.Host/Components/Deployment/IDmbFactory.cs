using System;
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
		/// Gets the next <see cref="IDmbProvider"/>.
		/// </summary>
		/// <param name="lockCount">The amount of locks to give the resulting <see cref="IDmbProvider"/>. It's <see cref="IDisposable.Dispose"/> must be called this many times to properly clean the job.</param>
		/// <returns>A new <see cref="IDmbProvider"/>.</returns>
		IDmbProvider LockNextDmb(int lockCount);

		/// <summary>
		/// Gets a <see cref="IDmbProvider"/> for a given <see cref="CompileJob"/>.
		/// </summary>
		/// <param name="compileJob">The <see cref="CompileJob"/> to make the <see cref="IDmbProvider"/> for.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a new <see cref="IDmbProvider"/> representing the <see cref="CompileJob"/> on success, <see langword="null"/> on failure.</returns>
		Task<IDmbProvider> FromCompileJob(CompileJob compileJob, CancellationToken cancellationToken);

		/// <summary>
		/// Deletes all compile jobs that are inactive in the Game folder.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task CleanUnusedCompileJobs(CancellationToken cancellationToken);
	}
}
