using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing the git repository.
	/// </summary>
	public interface IRepositoryClient
	{
		/// <summary>
		/// Get the repository's current status.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="RepositoryResponse"/>.</returns>
		Task<RepositoryResponse> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Update the repository.
		/// </summary>
		/// <param name="repository">The <see cref="RepositoryUpdateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="RepositoryResponse"/>.</returns>
		Task<RepositoryResponse> Update(RepositoryUpdateRequest repository, CancellationToken cancellationToken);

		/// <summary>
		/// Clones a <paramref name="repository"/>.
		/// </summary>
		/// <param name="repository">The <see cref="RepositoryCreateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="RepositoryResponse"/>/.</returns>
		Task<RepositoryResponse> Clone(RepositoryCreateRequest repository, CancellationToken cancellationToken);

		/// <summary>
		/// Deletes the repository.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="RepositoryResponse"/>.</returns>
		Task<RepositoryResponse> Delete(CancellationToken cancellationToken);
	}
}
