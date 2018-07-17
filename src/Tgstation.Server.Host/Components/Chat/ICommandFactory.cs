using System.Collections.Generic;
using Tgstation.Server.Host.Components.Chat.Commands;

namespace Tgstation.Server.Host.Components.Chat
{
    interface ICommandFactory
    {
		IReadOnlyList<Command> GenerateCommands();
    }
}
