using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment.Remote
{
	/// <summary>
	/// No-op implementation of <see cref="IRemoteDeploymentManager"/>.
	/// </summary>
	sealed class NoOpRemoteDeploymentManager : BaseRemoteDeploymentManager
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NoOpRemoteDeploymentManager"/> class.
		/// </summary>
		/// <param name="logger">The value of <see cref="BaseRemoteDeploymentManager.Logger"/>.</param>
		/// <param name="metadata">The value of <see cref="BaseRemoteDeploymentManager.Metadata"/>.</param>
		/// <param name="activationCallbacks">The activation callback <see cref="ConcurrentDictionary{TKey, TValue}"/> for the <see cref="BaseRemoteDeploymentManager"/>.</param>
		public NoOpRemoteDeploymentManager(
			ILogger<NoOpRemoteDeploymentManager> logger,
			Api.Models.Instance metadata,
			ConcurrentDictionary<long, Action<bool>> activationCallbacks)
			: base(logger, metadata, activationCallbacks)
		{
		}

		/// <inheritdoc />
		public override Task FailDeployment(Models.CompileJob compileJob, string errorMessage, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public override Task<IReadOnlyCollection<TestMerge>> RemoveMergedTestMerges(IRepository repository, Models.RepositorySettings repositorySettings, Models.RevisionInformation revisionInformation, CancellationToken cancellationToken)
			=> Task.FromResult<IReadOnlyCollection<TestMerge>>(Array.Empty<TestMerge>());

		/// <inheritdoc />
		public override Task StartDeployment(IGitRemoteInformation remoteInformation, Models.CompileJob compileJob, CancellationToken cancellationToken)
			=> Task.CompletedTask;

		/// <inheritdoc />
		protected override Task ApplyDeploymentImpl(Models.CompileJob compileJob, CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		protected override Task CommentOnTestMergeSource(
			Models.RepositorySettings repositorySettings,
			string remoteRepositoryOwner,
			string remoteRepositoryName,
			string comment,
			int testMergeNumber,
			CancellationToken cancellationToken)
			=> Task.CompletedTask;

		/// <inheritdoc />
		protected override string FormatTestMerge(
			Models.RepositorySettings repositorySettings,
			Models.CompileJob compileJob,
			TestMerge testMerge,
			string remoteRepositoryOwner,
			string remoteRepositoryName,
			bool updated)
			=> String.Empty;

		/// <inheritdoc />
		protected override Task MarkInactiveImpl(Models.CompileJob compileJob, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		protected override Task StageDeploymentImpl(Models.CompileJob compileJob, CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
