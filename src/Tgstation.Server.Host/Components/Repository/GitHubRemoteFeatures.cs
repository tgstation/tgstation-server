using System;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// GitHub <see cref="IGitRemoteFeatures"/>.
	/// </summary>
	sealed class GitHubRemoteFeatures : IGitRemoteFeatures
	{
		/// <inheritdoc />
		public string TestMergeRefSpecFormatter => "pull/{0}/head:{1}";

		/// <inheritdoc />
		public RemoteGitProvider? RemoteGitProvider => Api.Models.RemoteGitProvider.GitHub;

		/// <inheritdoc />
		public string RemoteRepositoryOwner { get; }

		/// <inheritdoc />
		public string RemoteRepositoryName { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHubRemoteFeatures"/> <see langword="class"/>.
		/// </summary>
		/// <param name="remoteUrl">The remote repository <see cref="Uri"/>.</param>
		public GitHubRemoteFeatures(Uri remoteUrl)
		{
			if (remoteUrl == null)
				throw new ArgumentNullException(nameof(remoteUrl));

			RemoteRepositoryOwner = remoteUrl.Segments[1].TrimEnd('/');
			RemoteRepositoryName = remoteUrl.Segments[2].TrimEnd('/');
		}
	}
}
