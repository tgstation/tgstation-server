using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing <see cref="DreamDaemonResponse"/>.
	/// </summary>
	public interface IDreamDaemonClient
	{
		/// <summary>
		/// Get the <see cref="DreamDaemonResponse"/> represented by the <see cref="IDreamDaemonClient"/>.
		/// </summary>
		/// <param name="profileMs">The amount of time to spend performance profiling.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="DreamDaemonResponse"/> information.</returns>
		ValueTask<DreamDaemonResponse> Read(ulong? profileMs = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Start <see cref="DreamDaemonResponse"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="JobResponse"/> of the running operation.</returns>
		ValueTask<JobResponse> Start(CancellationToken cancellationToken);

		/// <summary>
		/// Restart <see cref="DreamDaemonResponse"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="JobResponse"/> of the running operation.</returns>
		ValueTask<JobResponse> Restart(CancellationToken cancellationToken);

		/// <summary>
		/// Shutdown <see cref="DreamDaemonResponse"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="DreamDaemonResponse"/> information.</returns>
		ValueTask Shutdown(CancellationToken cancellationToken);

		/// <summary>
		/// Update <see cref="DreamDaemonResponse"/>. This may trigger a <see cref="Api.Models.Internal.DreamDaemonApiBase.SoftRestart"/>.
		/// </summary>
		/// <param name="dreamDaemon">The <see cref="DreamDaemonRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="DreamDaemonResponse"/> information.</returns>
		ValueTask<DreamDaemonResponse> Update(DreamDaemonRequest dreamDaemon, CancellationToken cancellationToken);

		/// <summary>
		/// Start a job to create a process dump of the active DreamDaemon executable.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="JobResponse"/> of the running operation.</returns>
		ValueTask<JobResponse> CreateDump(CancellationToken cancellationToken);
	}
}
