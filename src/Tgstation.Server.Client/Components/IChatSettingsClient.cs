using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing the chat bots
	/// </summary>
	public interface IChatSettingsClient
	{
		/// <summary>
		/// List the <see cref="ChatSettings"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of the <see cref="ChatSettings"/> of the server</returns>
		Task<IReadOnlyList<ChatSettings>> List(CancellationToken cancellationToken);

		/// <summary>
		/// Create a <see cref="ChatSettings"/>
		/// </summary>
		/// <param name="settings">The <see cref="ChatSettings"/> to create</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the new <see cref="ChatSettings"/></returns>
		Task<ChatSettings> Create(ChatSettings settings, CancellationToken cancellationToken);

		/// <summary>
		/// Updates a <see cref="ChatSettings"/> setttings
		/// </summary>
		/// <param name="settings">The <see cref="ChatSettings"/> to update</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the updated <see cref="ChatSettings"/></returns>
		Task<ChatSettings> Update(ChatSettings settings, CancellationToken cancellationToken);

		/// <summary>
		/// Delete a <see cref="ChatSettings"/>
		/// </summary>
		/// <param name="settings">The <see cref="ChatSettings"/> to delete</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Delete(ChatSettings settings, CancellationToken cancellationToken);
	}
}
