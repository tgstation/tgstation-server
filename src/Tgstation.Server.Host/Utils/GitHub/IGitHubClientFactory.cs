using System.Threading;
using System.Threading.Tasks;

using Octokit;

namespace Tgstation.Server.Host.Utils.GitHub
{
	/// <summary>
	/// For creating <see cref="IGitHubClient"/>s.
	/// </summary>
	public interface IGitHubClientFactory
	{
		/// <summary>
		/// Create a <see cref="IGitHubClient"/> client. Low rate limit unless the server's GitHubAccessToken is set to bypass it.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A new <see cref="IGitHubClient"/>.</returns>
		ValueTask<IGitHubClient> CreateClient(CancellationToken cancellationToken);

		/// <summary>
		/// Create a client with authentication using a personal access token.
		/// </summary>
		/// <param name="accessToken">The GitHub personal access token.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A new <see cref="IGitHubClient"/>.</returns>
		ValueTask<IGitHubClient> CreateClient(string accessToken, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a GitHub App client for an installation.
		/// </summary>
		/// <param name="pem">The private key <see cref="string"/>.</param>
		/// <param name="repositoryId">The GitHub repository ID.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="IGitHubClient"/> for the given <paramref name="repositoryId"/> or <see langword="null"/> if authentication failed.</returns>
		ValueTask<IGitHubClient?> CreateInstallationClient(string pem, long repositoryId, CancellationToken cancellationToken);
	}
}
