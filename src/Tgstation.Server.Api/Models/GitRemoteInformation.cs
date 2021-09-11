using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Provides information about remote providers.
	/// </summary>
	public sealed class GitRemoteInformation
	{
		/// <summary>
		/// The <see cref="RemoteGitProvider"/> in use by the repository.
		/// </summary>
		[EnumDataType(typeof(RemoteGitProvider))]
		public RemoteGitProvider RemoteGitProvider { get; init; }

		/// <summary>
		/// The owner of the remote repository.
		/// </summary>
		public string RepositoryOwner { get; init; }

		/// <summary>
		/// The name of the remote repository.
		/// </summary>
		public string RepositoryName { get; init; }

		/// <summary>
		/// Initializes a new instance of the <see cref="GitRemoteInformation"/> class.
		/// </summary>
		/// <param name="repositoryOwner">The value of <see cref="RepositoryOwner"/>.</param>
		/// <param name="repositoryName">The value of <see cref="RepositoryName"/>.</param>
		/// <param name="remoteGitProvider">The value of <see cref="RemoteGitProvider"/>.</param>
		public GitRemoteInformation(string repositoryOwner, string repositoryName, RemoteGitProvider remoteGitProvider)
		{
			RepositoryOwner = repositoryOwner ?? throw new ArgumentNullException(nameof(repositoryOwner));
			RepositoryName = repositoryName ?? throw new ArgumentNullException(nameof(repositoryName));
			RemoteGitProvider = remoteGitProvider;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GitRemoteInformation"/> class.
		/// </summary>
		[Obsolete("For JSON deserialization.", true)]
		public GitRemoteInformation()
		{
			RepositoryOwner = String.Empty;
			RepositoryName = String.Empty;
		}
	}
}
