using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// Creates and updates GitHub deployments.
	/// </summary>
	interface IGitHubDeploymentManager
	{
		/// <summary>
		/// Start a deployment for a given <paramref name="compileJob"/>.
		/// </summary>
		/// <param name="repository">The <see cref="IRepository"/> being deployed.</param>
		/// <param name="compileJob">The active <see cref="CompileJob"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task StartDeployment(IRepository repository, CompileJob compileJob, CancellationToken cancellationToken);

		/// <summary>
		/// Stage a given <paramref name="compileJob"/>'s deployment.
		/// </summary>
		/// <param name="compileJob">The staged <see cref="CompileJob"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task StageDeployment(
			CompileJob compileJob,
			CancellationToken cancellationToken);

		/// <summary>
		/// Stage a given <paramref name="compileJob"/>'s deployment.
		/// </summary>
		/// <param name="compileJob">The <see cref="CompileJob"/> being applied.</param>
		/// <param name="oldCompileJob">The currently active <see cref="CompileJob"/>, if any.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task ApplyDeployment(CompileJob compileJob, CompileJob oldCompileJob, CancellationToken cancellationToken);

		/// <summary>
		/// Fail a deployment for a given <paramref name="compileJob"/>.
		/// </summary>
		/// <param name="compileJob">The failed <see cref="CompileJob"/>.</param>
		/// <param name="errorMessage">The error message.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task FailDeployment(CompileJob compileJob, string errorMessage, CancellationToken cancellationToken);

		/// <summary>
		/// Mark the deplotment for a given <paramref name="compileJob"/> as inactive.
		/// </summary>
		/// <param name="compileJob">The inactive <see cref="CompileJob"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task MarkInactive(CompileJob compileJob, CancellationToken cancellationToken);
	}
}
