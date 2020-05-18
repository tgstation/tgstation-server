using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// For managing the compiler
	/// </summary>
	public interface IDreamMaker
	{
		/// <summary>
		/// Create and  a compile job and insert it into the database. Meant to be called by a <see cref="Jobs.IJobManager"/>.
		/// </summary>
		/// <param name="job">The running <see cref="Job"/>.</param>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the operation.</param>
		/// <param name="progressReporter">The <see cref="Action{T1}"/> to report compilation progress.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task DeploymentProcess(
			Job job,
			IDatabaseContext databaseContext,
			Action<int> progressReporter,
			CancellationToken cancellationToken);
	}
}