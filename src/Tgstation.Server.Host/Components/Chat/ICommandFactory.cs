using System.Collections.Generic;
using Tgstation.Server.Host.Components.Chat.Commands;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// Factory for built in <see cref="Command"/>s
	/// </summary>
	interface ICommandFactory
	{
		/// <summary>
		/// Generate builtin <see cref="Command"/>s
		/// </summary>
		/// <returns>A <see cref="IReadOnlyList{T}"/> of <see cref="Command"/>s</returns>
		IReadOnlyList<Command> GenerateCommands();
	}
}
