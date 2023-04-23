using Octokit;

namespace Tgstation.Server.Host.Utils
{
	/// <summary>
	/// For creating <see cref="IGitHubClient"/>s.
	/// </summary>
	public interface IGitHubClientFactory
	{
		/// <summary>
		/// Create a <see cref="IGitHubClient"/> client. Low rate limit unless the server's GitHubAccessToken is set to bypass it.
		/// </summary>
		/// <returns>A new <see cref="IGitHubClient"/>.</returns>
		IGitHubClient CreateClient();

		/// <summary>
		/// Create a client with authentication using a personal access token.
		/// </summary>
		/// <param name="accessToken">The GitHub personal access token.</param>
		/// <returns>A new <see cref="IGitHubClient"/>.</returns>
		IGitHubClient CreateClient(string accessToken);
	}
}
