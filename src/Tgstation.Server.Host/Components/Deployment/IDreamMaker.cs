using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// For managing the compiler.
	/// </summary>
	public interface IDreamMaker
	{
		/// <summary>
		/// Create and  a compile job and insert it into the database. Meant to be called by a <see cref="IJobManager"/>.
		/// </summary>
		/// <param name="job">The running <see cref="Job"/>.</param>
		/// <param name="databaseContextFactory">The <see cref="IDatabaseContextFactory"/> for the operation.</param>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> to report compilation progress.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask DeploymentProcess(
			Job job,
			IDatabaseContextFactory databaseContextFactory,
			JobProgressReporter progressReporter,
			CancellationToken cancellationToken);
	}
}
