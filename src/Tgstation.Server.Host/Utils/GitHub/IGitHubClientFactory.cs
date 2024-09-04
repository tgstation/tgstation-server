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
		/// Creates a GitHub client that will only be used for a given <paramref name="repositoryIdentifier"/>.
		/// </summary>
		/// <param name="accessString">The GitHub personal access token or TGS encoded app private key <see cref="string"/>.</param>
		/// <param name="repositoryIdentifier">The <see cref="RepositoryIdentifier"/> for the GitHub ID that the client will be used to connect to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="IGitHubClient"/> for the given <paramref name="repositoryIdentifier"/> or <see langword="null"/> if authentication failed.</returns>
		ValueTask<IGitHubClient?> CreateClientForRepository(string accessString, RepositoryIdentifier repositoryIdentifier, CancellationToken cancellationToken);

		/// <summary>
		/// Create an App (not installation) authenticated <see cref="IGitHubClient"/>.
		/// </summary>
		/// <param name="tgsEncodedAppPrivateKey">The TGS encoded app private key string.</param>
		/// <returns>A new app auth <see cref="IGitHubClient"/> for the given <paramref name="tgsEncodedAppPrivateKey"/> on success <see langword="null"/> on failure.</returns>
		IGitHubClient? CreateAppClient(string tgsEncodedAppPrivateKey);
	}
}
