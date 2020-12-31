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
		public string TestMergeRefSpecFormatter => throw new NotSupportedException();

		/// <inheritdoc />
		public string TestMergeLocalBranchNameFormatter => throw new NotSupportedException();

		/// <inheritdoc />
		public RemoteGitProvider? RemoteGitProvider => Api.Models.RemoteGitProvider.Unknown;

		/// <inheritdoc />
		public string RemoteRepositoryOwner => null;

		/// <inheritdoc />
		public string RemoteRepositoryName => null;

		/// <inheritdoc />
		public Task<Models.TestMerge> GetTestMerge(
			TestMergeParameters parameters,
			Api.Models.Internal.RepositorySettings repositorySettings,
			CancellationToken cancellationToken) => throw new NotSupportedException();
	}
}
