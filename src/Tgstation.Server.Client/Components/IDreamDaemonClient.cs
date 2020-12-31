using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing <see cref="DreamDaemon"/>
	/// </summary>
	public interface IDreamDaemonClient
	{
		/// <summary>
		/// Get the <see cref="DreamDaemon"/> represented by the <see cref="IDreamDaemonClient"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="DreamDaemon"/> information</returns>
		Task<DreamDaemon> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Start <see cref="DreamDaemon"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Job"/> of the running operation</returns>
		Task<Job> Start(CancellationToken cancellationToken);

		/// <summary>
		/// Restart <see cref="DreamDaemon"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Job"/> of the running operation</returns>
		Task<Job> Restart(CancellationToken cancellationToken);

		/// <summary>
		/// Shutdown <see cref="DreamDaemon"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="DreamDaemon"/> information</returns>
		Task Shutdown(CancellationToken cancellationToken);

		/// <summary>
		/// Update <see cref="DreamDaemon"/>. This may trigger <see cref="DreamDaemon.SoftRestart"/>
		/// </summary>
		/// <param name="dreamDaemon">The <see cref="DreamDaemon"/> to update</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="DreamDaemon"/> information</returns>
		Task<DreamDaemon> Update(DreamDaemon dreamDaemon, CancellationToken cancellationToken);

		/// <summary>
		/// Start a job to create a process dump of the active DreamDaemon executable.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Job"/> of the running operation.</returns>
		Task<Job> CreateDump(CancellationToken cancellationToken);
	}
}
