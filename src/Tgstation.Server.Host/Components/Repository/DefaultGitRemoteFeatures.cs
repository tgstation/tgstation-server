using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// The <see cref="IGitRemoteFeatures"/> used for unknown providers.
	/// </summary>
	sealed class DefaultGitRemoteFeatures : IGitRemoteFeatures
	{
		/// <inheritdoc />
		public string TestMergeRefSpecFormatter => null;

		/// <inheritdoc />
		public RemoteGitProvider? RemoteGitProvider => Api.Models.RemoteGitProvider.Unknown;

		/// <inheritdoc />
		public string RemoteRepositoryOwner => null;

		/// <inheritdoc />
		public string RemoteRepositoryName => null;
	}
}
