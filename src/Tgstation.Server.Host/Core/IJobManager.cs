using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Manages the runtime of <see cref="Job"/>s
	/// </summary>
	public interface IJobManager : IHostedService
	{
		/// <summary>
		/// Get the <see cref="Api.Models.Job.Progress"/> for a job
		/// </summary>
		int? JobProgress(Job job);

		/// <summary>
		/// Registers a given <see cref="Job"/> and begins running it
		/// </summary>
		/// <param name="job">The <see cref="Job"/></param>
		/// <param name="operation">The operation to run taking the started <see cref="Job"/>, a <see cref="IServiceProvider"/> progress reporter <see cref="Action{T1}"/> and a <see cref="CancellationToken"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing a running operation</returns>
		Task RegisterOperation(Job job, Func<Job, IServiceProvider, Action<int>, CancellationToken, Task> operation, CancellationToken cancellationToken);

		/// <summary>
		/// Cancels a give <paramref name="job"/>
		/// </summary>
		/// <param name="job">The <see cref="Job"/> to cancel</param>
		/// <param name="user">The <see cref="User"/> who cancelled the <paramref name="job"/></param>
		/// <param name="blocking">If the operation should wait until the job exits before completing</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing a running operation</returns>
		Task CancelJob(Job job, User user, bool blocking, CancellationToken cancellationToken);
	}
}
