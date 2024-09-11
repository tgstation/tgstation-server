namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Indicates the remote git host.
	/// </summary>
	public enum RemoteGitProvider // Note if adding more: There's an assumption that all but unknown can derive a slug
	{
		/// <summary>
		/// Unknown remote git provider.
		/// </summary>
		Unknown,

		/// <summary>
		/// Remote provider is GitHub.com.
		/// </summary>
		GitHub,

		/// <summary>
		/// Remote provider is GitLab.com.
		/// </summary>
		GitLab,
	}
}
