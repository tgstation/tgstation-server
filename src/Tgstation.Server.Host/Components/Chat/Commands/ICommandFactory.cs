using System.Collections.Generic;

#nullable disable

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// Factory for built in <see cref="ICommand"/>s.
	/// </summary>
	interface ICommandFactory
	{
		/// <summary>
		/// Generate builtin <see cref="ICommand"/>s.
		/// </summary>
		/// <returns>A <see cref="IReadOnlyList{T}"/> of <see cref="ICommand"/>s.</returns>
		IReadOnlyList<ICommand> GenerateCommands();
	}
}
