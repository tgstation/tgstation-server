using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Api.Hubs
{
	/// <summary>
	/// SignalR client methods for receiving <see cref="JobResponse"/>s.
	/// </summary>
	public interface IJobsHub : IErrorHandlingHub
	{
		/// <summary>
		/// Push a <paramref name="job"/> update to the client.
		/// </summary>
		/// <param name="job">The <see cref="JobResponse"/> to push.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		Task ReceiveJobUpdate(JobResponse job, CancellationToken cancellationToken);
	}
}
