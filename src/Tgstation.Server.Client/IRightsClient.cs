using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// <see cref="Api"/> client that has a <typeparamref name="TRights"/> bitset
	/// </summary>
	/// <typeparam name="TRights">The <see cref="Api.Rights"/> for the <see cref="IRightsClient{TRights}"/></typeparam>
	public interface IRightsClient<TRights>
	{
		/// <summary>
		/// Get the <typeparamref name="TRights"/> for the <see cref="IRightsClient{TRights}"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <typeparamref name="TRights"/> for the <see cref="IRightsClient{TRights}"/></returns>
		Task<TRights> Rights(CancellationToken cancellationToken);
	}
}
