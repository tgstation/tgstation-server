using System;
using TGS.Server.Chat.Commands;

namespace TGS.Server.Chat
{
	/// <summary>
	/// Used for populating a <see cref="CommandInfo"/> structure
	/// </summary>
	sealed class PopulateCommandInfoEventArgs : EventArgs
	{
		/// <summary>
		/// The <see cref="CommandInfo"/> to populate
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
