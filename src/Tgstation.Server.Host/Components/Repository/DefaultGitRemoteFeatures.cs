using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// The <see cref="IGitRemoteFeatures"/> used for unknown providers.
	/// </summary>
	sealed class DefaultGitRemoteFeatures : IGitRemoteFeatures
	{
		/// <inheritdoc />
		public string GetTestMergeRefSpec(TestMergeParameters parameters) => throw new NotSupportedException();

		/// <inheritdoc />
		public string GetTestMergeLocalBranchName(TestMergeParameters parameters) => throw new NotSupportedException();

		/// <inheritdoc />
		public RemoteGitProvider? RemoteGitProvider => Api.Models.RemoteGitProvider.Unknown;

		/// <inheritdoc />
		public string? RemoteRepositoryOwner => null;

		/// <inheritdoc />
		public string? RemoteRepositoryName => null;

		/// <inheritdoc />
		public ValueTask<Models.TestMerge> GetTestMerge(
			TestMergeParameters parameters,
			RepositorySettings repositorySettings,
			CancellationToken cancellationToken) => throw new NotSupportedException();

		/// <inheritdoc />
		public ValueTask<string?> TransformRepositoryPassword(string? rawPassword, CancellationToken cancellationToken)
			=> ValueTask.FromResult(rawPassword);

		/// <inheritdoc />
		public Uri GetRemoteUrl(string owner, string name) => throw new NotSupportedException();

		/// <inheritdoc />
		public (string Owner, string Name) GetRepositoryOwnerAndName(TestMergeParameters parameters) => throw new NotSupportedException();
	}
}
