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
		/// List the <see cref="JobResponse"/>s in the <see cref="Instance"/>
		/// </summary>
		/// <param name="paginationSettings">The optional <see cref="PaginationSettings"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of the <see cref="JobResponse"/> <see cref="EntityId"/>s in the <see cref="Instance"/></returns>
		Task<IReadOnlyList<JobResponse>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken);

		/// <summary>
		/// List the active <see cref="JobResponse"/>s in the <see cref="Instance"/>
		/// </summary>
		/// <param name="paginationSettings">The optional <see cref="PaginationSettings"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of the active <see cref="JobResponse"/>s in the <see cref="Instance"/></returns>
		Task<IReadOnlyList<JobResponse>> ListActive(PaginationSettings? paginationSettings, CancellationToken cancellationToken);

		/// <summary>
		/// Get a <paramref name="job"/>
		/// </summary>
		/// <param name="job">The <see cref="JobResponse"/>'s <see cref="EntityId"/> to get</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="JobResponse"/></returns>
		Task<JobResponse> GetId(EntityId job, CancellationToken cancellationToken);

		/// <summary>
		/// Cancels a <paramref name="job"/>
		/// </summary>
		/// <param name="job">The <see cref="JobResponse"/> to cancel</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Cancel(JobResponse job, CancellationToken cancellationToken);
	}
}
