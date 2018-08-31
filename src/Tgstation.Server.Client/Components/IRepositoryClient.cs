using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing the <see cref="Repository"/>
	/// </summary>
	public interface IRepositoryClient
	{
		/// <summary>
		/// Get the <see cref="Repository"/> represented by the <see cref="IRepositoryClient"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Repository"/></returns>
		Task<Repository> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Update the <see cref="Repository"/>
		/// </summary>
		/// <param name="repository">The <see cref="Repository"/> to update</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the updated <see cref="Repository"/></returns>
		Task<Repository> Update(Repository repository, CancellationToken cancellationToken);

		/// <summary>
		/// Clones a <paramref name="repository"/>
		/// </summary>
		/// <param name="repository">The <see cref="Repository"/> to clone</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the updated <see cref="Repository"/></returns>
		Task<Repository> Clone(Repository repository, CancellationToken cancellationToken);

		/// <summary>
		/// Deletes the <see cref="Repository"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the updated <see cref="Repository"/></returns>
		Task<Repository> Delete(CancellationToken cancellationToken);
	}
}
