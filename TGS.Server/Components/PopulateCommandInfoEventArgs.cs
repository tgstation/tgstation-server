using System;

namespace TGS.Server.ChatCommands
{
	/// <summary>
	/// Used for populating a <see cref="ChatCommands.CommandInfo"/> structure
	/// </summary>
	sealed class PopulateCommandInfoEventArgs : EventArgs
	{
		/// <summary>
		/// The <see cref="ChatCommands.CommandInfo"/> to populate
		/// </summary>
		public CommandInfo CommandInfo{ get { return _commandInfo; } }

		/// <summary>
		/// Backing variable for <see cref="CommandInfo"/>
		/// </summary>
		readonly CommandInfo _commandInfo;

		/// <summary>
		/// Construct a <see cref="PopulateCommandInfoEventArgs"/>
		/// </summary>
		/// <param name="ci">The valuse of <see cref="_commandInfo"/></param>
		public PopulateCommandInfoEventArgs(CommandInfo ci)
		{
			_commandInfo = ci;
		}
	}
}
