using System;
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
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Repository"/> represented by the <see cref="IRepositoryClient"/></returns>
		Task<Repository> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Update the <see cref="Repository"/>
		/// </summary>
		/// <param name="repository">The <see cref="Repository"/> to update</param>
		/// <param name="progressCallback">Optional action to take when progress is reported. Will either not be called for some operations, or be called with the numbers 1-100</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Update(Repository repository, Action<int> progressCallback, CancellationToken cancellationToken);
	}
}
