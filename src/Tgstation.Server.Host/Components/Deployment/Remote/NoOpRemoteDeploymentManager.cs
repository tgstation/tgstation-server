using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment.Remote
{
	/// <summary>
	/// No-op implementation of <see cref="IRemoteDeploymentManager"/>.
	/// </summary>
	sealed class NoOpRemoteDeploymentManager : IRemoteDeploymentManager
	{
		/// <inheritdoc />
		public Task ApplyDeployment(
			CompileJob compileJob,
			CompileJob oldCompileJob,
			CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public Task FailDeployment(
			CompileJob compileJob,
			string errorMessage,
			CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public Task MarkInactive(
			CompileJob compileJob,
			CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public Task PostDeploymentComments(
			CompileJob compileJob,
			RevisionInformation previousRevisionInformation,
			RepositorySettings repositorySettings,
			string repoOwner,
			string repoName,
			CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public Task<IReadOnlyCollection<RevInfoTestMerge>> RemoveMergedTestMerges(
			IRepository repository,
			RepositorySettings repositorySettings,
			RevisionInformation revisionInformation,
			CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<RevInfoTestMerge>>(Array.Empty<RevInfoTestMerge>());

		/// <inheritdoc />
		public Task StageDeployment(CompileJob compileJob, CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public Task StartDeployment(
			Api.Models.Internal.IGitRemoteInformation remoteInformation,
			CompileJob compileJob,
			CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
