using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Jobs
{
	/// <summary>
	/// Manages the runtime of <see cref="Job"/>s.
	/// </summary>
	public interface IJobManager
	{
		/// <summary>
		/// Set the <see cref="JobResponse.Progress"/> and <see cref="JobResponse.Stage"/> for a given <paramref name="apiResponse"/>.
		/// </summary>
		/// <param name="apiResponse">The <see cref="JobResponse"/> to update.</param>
		void SetJobProgress(JobResponse apiResponse);

		/// <summary>
		/// Registers a given <see cref="Job"/> and begins running it.
		/// </summary>
		/// <param name="job">The <see cref="Job"/>. Should at least have <see cref="Job.Instance"/> and <see cref="Api.Models.Internal.Job.Description"/>. If <see cref="Job.StartedBy"/> is <see langword="null"/>, the TGS user will be used.</param>
		/// <param name="operation">The <see cref="JobEntrypoint"/> for the <paramref name="job"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing a running operation.</returns>
		ValueTask RegisterOperation(Job job, JobEntrypoint operation, CancellationToken cancellationToken);

		/// <summary>
		/// Wait for a given <paramref name="job"/> to complete.
		/// </summary>
		/// <param name="job">The <see cref="Job"/> to wait for. </param>
		/// <param name="canceller">The <see cref="User"/> to cancel the <paramref name="job"/>. If <see langword="null"/> the TGS user will be used.</param>
		/// <param name="jobCancellationToken">A <see cref="CancellationToken"/> that will cancel the <paramref name="job"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the <see cref="Job"/>.</returns>
		ValueTask WaitForJobCompletion(Job job, User canceller, CancellationToken jobCancellationToken, CancellationToken cancellationToken);

		/// <summary>
		/// Cancels a give <paramref name="job"/>.
		/// </summary>
		/// <param name="job">The <see cref="Job"/> to cancel.</param>
		/// <param name="user">The <see cref="User"/> who cancelled the <paramref name="job"/>. If <see langword="null"/> the TGS user will be used.</param>
		/// <param name="blocking">If the operation should wait until the job exits before completing.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the updated <paramref name="job"/> if it was cancelled, <see langword="null"/> if it couldn't be found.</returns>
		ValueTask<Job> CancelJob(Job job, User user, bool blocking, CancellationToken cancellationToken);
	}
}
