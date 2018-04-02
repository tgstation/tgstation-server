using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Basic <see cref="Api"/> client
	/// </summary>
	/// <typeparam name="TRights">The <see cref="Api.Rights"/> for the <see cref="IClient{TRights, TModel}"/></typeparam>
	/// <typeparam name="TModel">Which of the <see cref="Api.Models"/> the <see cref="IClient{TRights, TModel}"/> represents</typeparam>
	public interface IClient<TRights, TModel>
	{
		/// <summary>
		/// Get the <typeparamref name="TRights"/> for the <see cref="IClient{TRights, TModel}"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <typeparamref name="TRights"/> for the <see cref="IClient{TRights, TModel}"/></returns>
		Task<TRights> Rights(CancellationToken cancellationToken);

		/// <summary>
		/// Read the <typeparamref name="TModel"/> of the <see cref="IClient{TRights, TModel}"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>The <typeparamref name="TModel"/> of the <see cref="IClient{TRights, TModel}"/></returns>
		Task<TModel> Read(CancellationToken cancellationToken);
	}
}
