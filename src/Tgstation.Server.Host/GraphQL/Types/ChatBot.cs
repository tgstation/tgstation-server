using System;
using System.Threading.Tasks;

using HotChocolate.Types.Relay;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a connection to a chat <see cref="Provider"/>.
	/// </summary>
	[Node]
	public sealed class ChatBot : Entity
	{
		/// <summary>
		/// Node resolver for <see cref="ChatBot"/>.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/> of the <see cref="ChatBot"/> to retrieve.</param>
		/// <returns>The <see cref="ChatBot"/> with <paramref name="id"/> if it exists, <see langword="null"/> otherwise.</returns>
		public static ValueTask<ChatBot?> GetChatBot(long id)
			=> throw new NotImplementedException();

		/// <summary>
		/// The <see cref="ChatProvider"/> the chat bot uses.
		/// </summary>
		public required ChatProvider Provider { get; init; }
	}
}
