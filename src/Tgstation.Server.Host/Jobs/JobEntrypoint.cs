using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Jobs
{
	/// <summary>
	/// Entrypoint for running a given job.
	/// </summary>
	/// <param name="instance">The <see cref="IInstanceCore"/> the job is running on. <see langword="null"/> only when performing an instance move operation.</param>
	/// <param name="databaseContextFactory">The <see cref="IDatabaseContextFactory"/> for the operation.</param>
	/// <param name="job">The running <see cref="Job"/>.</param>
	/// <param name="progressReporter">A <see cref="Action{T1}"/> that will update the progress of the job.</param>
	/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
	/// <returns>A <see cref="Task"/> representing the running operation.</returns>
	public delegate Task JobEntrypoint(
		IInstanceCore instance,
		IDatabaseContextFactory databaseContextFactory,
		Job job,
		Action<int> progressReporter,
		CancellationToken cancellationToken);
}
