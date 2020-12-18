using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing the chat bots
	/// </summary>
	public interface IChatBotsClient
	{
		/// <summary>
		/// List the <see cref="ChatBot"/>s
		/// </summary>
		/// <param name="paginationSettings">The optional <see cref="PaginationSettings"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of the <see cref="ChatBot"/> of the server</returns>
		Task<IReadOnlyList<ChatBot>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken);

		/// <summary>
		/// Create a <see cref="ChatBot"/>
		/// </summary>
		/// <param name="settings">The <see cref="ChatBot"/> to create</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the new <see cref="ChatBot"/></returns>
		Task<ChatBot> Create(ChatBot settings, CancellationToken cancellationToken);

		/// <summary>
		/// Updates a <see cref="ChatBot"/>'s setttings
		/// </summary>
		/// <param name="settings">The <see cref="ChatBot"/> to update</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the updated <see cref="ChatBot"/></returns>
		Task<ChatBot> Update(ChatBot settings, CancellationToken cancellationToken);

		/// <summary>
		/// Get a <see cref="ChatBot"/>'s setttings
		/// </summary>
		/// <param name="settings">The <see cref="ChatBot"/> to get</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="ChatBot"/></returns>
		Task<ChatBot> GetId(ChatBot settings, CancellationToken cancellationToken);

		/// <summary>
		/// Delete a <see cref="ChatBot"/>
		/// </summary>
		/// <param name="settings">The <see cref="ChatBot"/> to delete</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Delete(ChatBot settings, CancellationToken cancellationToken);
	}
}
