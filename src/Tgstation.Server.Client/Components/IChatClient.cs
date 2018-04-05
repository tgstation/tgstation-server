using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing the chat bots
	/// </summary>
	public interface IChatClient : IRightsClient<ChatRights>
	{
		/// <summary>
		/// Get the <see cref="Chat"/> represented by the <see cref="IChatClient"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Chat"/> represented by the <see cref="IChatClient"/></returns>
		Task<Chat> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Updates the <see cref="Chat"/> setttings
		/// </summary>
		/// <param name="chat">The <see cref="Chat"/> to update</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Update(Chat chat, CancellationToken cancellationToken);
	}
}
