using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// <see cref="IServerClient"/> for managing <see cref="Token"/>s
	/// </summary>
	public interface ITokenClient: IRightsClient<TokenRights>
	{
		/// <summary>
		/// Generate a new <see cref="Token"/> for the connected user, expiring the current one if applicable
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the new <see cref="Token"/></returns>
		Task<Token> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Gets all active <see cref="Token"/>s for the <see cref="IServerClient"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting a <see cref="IReadOnlyList{T}"/> of active <see cref="Token"/>s for the <see cref="IServerClient"/></returns>
		Task<IReadOnlyList<Token>> GetClientInfos(CancellationToken cancellationToken);

		/// <summary>
		/// Gets all active <see cref="Token"/>s for all users
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting a <see cref="IReadOnlyDictionary{TKey, TValue}"/> of active <see cref="Token"/>s for the <see cref="IServerClient"/> keyed by username</returns>
		Task<IReadOnlyDictionary<string, Token>> GetAllInfos(CancellationToken cancellationToken);

		/// <summary>
		/// Invalidate the specified active <paramref name="token"/>
		/// </summary>
		/// <param name="token">The <see cref="Token"/> to invalidate</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Invalidate(Token token, CancellationToken cancellationToken);

		/// <summary>
		/// Invalidate all active <see cref="Token"/>s for a <see cref="IServerClient"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task InvalidateAllClient(CancellationToken cancellationToken);

		/// <summary>
		/// Invalidate all active <see cref="Token"/>s
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task InvalidateAll(CancellationToken cancellationToken);
	}
}
