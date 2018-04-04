using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// Access to running jobs
	/// </summary>
    public interface IJobsClient
    {
		/// <summary>
		/// List the active jobs the user can view
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of active <see cref="Job"/>s the user can view</returns>
		Task<IReadOnlyList<Job>> List(CancellationToken cancellationToken);

		/// <summary>
		/// Cancels a <paramref name="job"/>
		/// </summary>
		/// <param name="job">The <see cref="Job"/> to cancel</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Cancel(Job job, CancellationToken cancellationToken);
    }
}
