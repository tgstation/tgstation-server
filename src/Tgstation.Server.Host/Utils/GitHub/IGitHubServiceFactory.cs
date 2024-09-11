using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Utils.GitHub
{
	/// <summary>
	/// Factory for <see cref="IGitHubService"/>s.
	/// </summary>
	public interface IGitHubServiceFactory
	{
		/// <summary>
		/// Create a <see cref="IGitHubService"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="IGitHubService"/>.</returns>
		public ValueTask<IGitHubService> CreateService(CancellationToken cancellationToken);

		/// <summary>
		/// Create an <see cref="IAuthenticatedGitHubService"/>.
		/// </summary>
		/// <param name="accessToken">The access token to use for communication with GitHub.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="IAuthenticatedGitHubService"/>.</returns>
		public ValueTask<IAuthenticatedGitHubService> CreateService(string accessToken, CancellationToken cancellationToken);

		/// <summary>
		/// Create an <see cref="IAuthenticatedGitHubService"/>.
		/// </summary>
		/// <param name="accessString">The access token to use for communication with GitHub.</param>
		/// <param name="repositoryIdentifier">The <see cref="RepositoryIdentifier"/> for the repository the service will be talking with.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="IAuthenticatedGitHubService"/>.</returns>
		public ValueTask<IAuthenticatedGitHubService?> CreateService(string accessString, RepositoryIdentifier repositoryIdentifier, CancellationToken cancellationToken);
	}
}
