using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment.Remote
{
	/// <summary>
	/// Creates and updates remote deployments.
	/// </summary>
	interface IRemoteDeploymentManager
	{
		/// <summary>
		/// Start a deployment for a given <paramref name="compileJob"/>.
		/// </summary>
		/// <param name="remoteInformation">The <see cref="Api.Models.Internal.IGitRemoteInformation"/> of the repository being deployed.</param>
		/// <param name="compileJob">The active <see cref="CompileJob"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask StartDeployment(
			Api.Models.Internal.IGitRemoteInformation remoteInformation,
			CompileJob compileJob,
			CancellationToken cancellationToken);

		/// <summary>
		/// Stage a given <paramref name="compileJob"/>'s deployment.
		/// </summary>
		/// <param name="compileJob">The staged <see cref="CompileJob"/>.</param>
		/// <param name="activationCallback">An optional <see cref="Action{T1}"/> to be called when the <see cref="CompileJob"/> becomes active or is discarded with <see langword="true"/> or <see langword="false"/> respectively.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask StageDeployment(
			CompileJob compileJob,
			Action<bool> activationCallback,
			CancellationToken cancellationToken);

		/// <summary>
		/// Stage a given <paramref name="compileJob"/>'s deployment.
		/// </summary>
		/// <param name="compileJob">The <see cref="CompileJob"/> being applied.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask ApplyDeployment(CompileJob compileJob, CancellationToken cancellationToken);

		/// <summary>
		/// Fail a deployment for a given <paramref name="compileJob"/>.
		/// </summary>
		/// <param name="compileJob">The failed <see cref="CompileJob"/>.</param>
		/// <param name="errorMessage">The error message.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask FailDeployment(CompileJob compileJob, string errorMessage, CancellationToken cancellationToken);

		/// <summary>
		/// Mark the deplotment for a given <paramref name="compileJob"/> as inactive.
		/// </summary>
		/// <param name="compileJob">The inactive <see cref="CompileJob"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		ValueTask MarkInactive(CompileJob compileJob, CancellationToken cancellationToken);

		/// <summary>
		/// Post deployment comments to the test merge ticket.
		/// </summary>
		/// <param name="compileJob">The deployed <see cref="CompileJob"/>.</param>
		/// <param name="previousRevisionInformation">The <see cref="RevisionInformation"/> of the previous deployment.</param>
		/// <param name="repositorySettings">The <see cref="RepositorySettings"/>.</param>
		/// <param name="repoOwner">The GitHub repostiory owner.</param>
		/// <param name="repoName">The GitHub repostiory name.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask PostDeploymentComments(
			CompileJob compileJob,
			RevisionInformation previousRevisionInformation,
			RepositorySettings repositorySettings,
			string repoOwner,
			string repoName,
			CancellationToken cancellationToken);

		/// <summary>
		/// Get the updated list of <see cref="TestMerge"/>s for an origin merge.
		/// </summary>
		/// <param name="repository">The <see cref="IRepository"/> to use.</param>
		/// <param name="repositorySettings">The <see cref="RepositorySettings"/>.</param>
		/// <param name="revisionInformation">The current <see cref="RevisionInformation"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IReadOnlyCollection{T}"/> of <see cref="TestMerge"/>s that should remain the new <see cref="RevisionInformation"/>.</returns>
		ValueTask<IReadOnlyCollection<TestMerge>> RemoveMergedTestMerges(
			IRepository repository,
			RepositorySettings repositorySettings,
			RevisionInformation revisionInformation,
			CancellationToken cancellationToken);
	}
}
