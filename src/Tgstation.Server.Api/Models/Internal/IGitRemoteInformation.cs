namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Provides information about remote providers.
	/// </summary>
	public interface IGitRemoteInformation
	{
		/// <summary>
		/// The <see cref="Models.RemoteGitProvider"/> in use by the repository.
		/// </summary>
		public RemoteGitProvider? RemoteGitProvider { get; }

		/// <summary>
		/// If <see cref="RemoteGitProvider"/> is not <see cref="RemoteGitProvider.Unknown"/> this will be set with the owner of the repository.
		/// </summary>
		public string? RemoteRepositoryOwner { get; }

		/// <summary>
		/// If <see cref="RemoteGitProvider"/> is not <see cref="RemoteGitProvider.Unknown"/> this will be set with the name of the repository.
		/// </summary>
		public string? RemoteRepositoryName { get; }
	}
}
