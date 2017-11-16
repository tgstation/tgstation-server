using System;
using TGS.Server.ChatCommands;

namespace TGS.Server.Components
{
	sealed class PopulateCommandInfoEventArgs : EventArgs
	{
		public CommandInfo CommandInfo{ get { return _commandInfo; } }

		readonly CommandInfo _commandInfo;

		public PopulateCommandInfoEventArgs(CommandInfo ci)
		{
			_commandInfo = ci;
		}
	}
}
