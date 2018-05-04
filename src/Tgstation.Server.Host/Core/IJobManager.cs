using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Manages the runtime of <see cref="Job"/>s
	/// </summary>
    public interface IJobManager
	{
		/// <summary>
		/// Registers a given <see cref="Job"/> and begins running it
		/// </summary>
		/// <param name="job">The <see cref="Job"/></param>
		/// <param name="operation">The operation to run</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing a running operation</returns>
		Task RegisterOperation(Job job, Func<IServiceProvider, CancellationToken, Task> operation, CancellationToken cancellationToken);

		/// <summary>
		/// Wait for a given <paramref name="job"/> to complete
		/// </summary>
		/// <param name="job">The <see cref="Job"/> to wait for</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing a running operation</returns>
		Task WaitForJob(Job job, CancellationToken cancellationToken);

		/// <summary>
		/// Cancels a give <paramref name="job"/>
		/// </summary>
		/// <param name="job">The <see cref="Job"/> to cancel</param>
		void CancelJob(Job job);
    }
}
