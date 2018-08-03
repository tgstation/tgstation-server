using Octokit;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// For creating <see cref="IGitHubClient"/>s
	/// </summary>
	public interface IGitHubClientFactory
	{
		/// <summary>
		/// Create a client with anonymous authentication. Low rate limit
		/// </summary>
		/// <returns>A new <see cref="IGitHubClient"/></returns>
		IGitHubClient CreateClient();

		/// <summary>
		/// Create a client with authentication using a personal access token
		/// </summary>
		/// <param name="accessToken">The GitHub personal access token</param>
		/// <returns>A new <see cref="IGitHubClient"/></returns>
		IGitHubClient CreateClient(string accessToken);
	}
}