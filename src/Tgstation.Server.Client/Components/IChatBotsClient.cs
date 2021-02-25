using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing the chat bots
	/// </summary>
	public interface IChatBotsClient
	{
		/// <summary>
		/// List the chat bots.
		/// </summary>
		/// <param name="paginationSettings">The optional <see cref="PaginationSettings"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of the <see cref="ChatBotResponse"/>s.</returns>
		Task<IReadOnlyList<ChatBotResponse>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken);

		/// <summary>
		/// Create a chat bot.
		/// </summary>
		/// <param name="settings">The <see cref="ChatBotCreateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="ChatBotResponse"/> of the newly created chat bot.</returns>
		Task<ChatBotResponse> Create(ChatBotCreateRequest settings, CancellationToken cancellationToken);

		/// <summary>
		/// Updates a chat bot's setttings.
		/// </summary>
		/// <param name="settings">The <see cref="ChatBotUpdateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the updated chat bot's <see cref="ChatBotResponse"/>.</returns>
		Task<ChatBotResponse> Update(ChatBotUpdateRequest settings, CancellationToken cancellationToken);

		/// <summary>
		/// Get a specific chat bot's settings.
		/// </summary>
		/// <param name="settingsId">The <see cref="EntityId"/> of the chat bot to get.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="ChatBotResponse"/>.</returns>
		Task<ChatBotResponse> GetId(EntityId settingsId, CancellationToken cancellationToken);

		/// <summary>
		/// Delete a chat bot.
		/// </summary>
		/// <param name="settingsId">The <see cref="EntityId"/> of the chat bot to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Delete(EntityId settingsId, CancellationToken cancellationToken);
	}
}
