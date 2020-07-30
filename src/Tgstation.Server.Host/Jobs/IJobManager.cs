using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Jobs
{
	/// <summary>
	/// Manages the runtime of <see cref="Job"/>s
	/// </summary>
	public interface IJobManager : IHostedService
	{
		/// <summary>
		/// Get the <see cref="Api.Models.Job.Progress"/> for a <paramref name="job"/>
		/// </summary>
		/// <param name="job">The <see cref="Job"/> to get <see cref="Api.Models.Job.Progress"/> for</param>
		/// <returns>The <see cref="Api.Models.Job.Progress"/> of <paramref name="job"/></returns>
		int? JobProgress(Job job);

		/// <summary>
		/// Registers a given <see cref="Job"/> and begins running it
		/// </summary>
		/// <param name="job">The <see cref="Job"/>. Should at least have <see cref="Job.Instance"/> and <see cref="Api.Models.Internal.Job.Description"/>. If <see cref="Job.StartedBy"/> is <see langword="null"/>, the TGS user will be used.</param>
		/// <param name="operation">The <see cref="JobEntrypoint"/> for the <paramref name="job"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing a running operation</returns>
		Task RegisterOperation(Job job, JobEntrypoint operation, CancellationToken cancellationToken);

		/// <summary>
		/// Wait for a given <paramref name="job"/> to complete
		/// </summary>
		/// <param name="job">The <see cref="Job"/> to wait for </param>
		/// <param name="canceller">The <see cref="User"/> to cancel the <paramref name="job"/>. If <see langword="null"/> the TGS user will be used.</param>
		/// <param name="jobCancellationToken">A <see cref="CancellationToken"/> that will cancel the <paramref name="job"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the <see cref="Job"/></returns>
#pragma warning disable CA1068 // CancellationToken parameters must come last https://github.com/dotnet/roslyn-analyzers/issues/1816
		Task WaitForJobCompletion(Job job, User canceller, CancellationToken jobCancellationToken, CancellationToken cancellationToken);
#pragma warning restore CA1068 // CancellationToken parameters must come last

		/// <summary>
		/// Cancels a give <paramref name="job"/>
		/// </summary>
		/// <param name="job">The <see cref="Job"/> to cancel</param>
		/// <param name="user">The <see cref="User"/> who cancelled the <paramref name="job"/>. If <see langword="null"/> the TGS user will be used.</param>
		/// <param name="blocking">If the operation should wait until the job exits before completing</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the updated <paramref name="job"/> if it was cancelled, <see langword="null"/> if it couldn't be found.</returns>
		Task<Job> CancelJob(Job job, User user, bool blocking, CancellationToken cancellationToken);

		/// <summary>
		/// Activate the <see cref="IJobManager"/>.
		/// </summary>
		void Activate();
	}
}
