using System.Collections.Generic;
using System.Threading;
using TGServiceInterface;

namespace TGServerService.ChatCommands
{
	/// <summary>
	/// A command heard by a <see cref="ChatProviders.IChatProvider"/>
	/// </summary>
	abstract class ChatCommand : Command
	{
		/// <summary>
		/// <see cref="CommandInfo"/> for the <see cref="ChatCommand"/>
		/// </summary>
		public static ThreadLocal<CommandInfo> CommandInfo { get; private set; } = new ThreadLocal<CommandInfo>();
		/// <summary>
		/// If set to <see langword="true"/>, the <see cref="ChatCommand"/> cannot be invoked by a non-admin or outside an admin chat channel
		/// </summary>
		public bool RequiresAdmin { get; protected set; }
		/// <summary>
		/// Shorthand for accessing <see cref="CommandInfo.Server"/>
		/// </summary>
		protected ServerInstance Instance { get { return CommandInfo.Value.Server; } }

		/// <inheritdoc />
		public override ExitCode DoRun(IList<string> parameters)
		{
			if (RequiresAdmin)
			{
				var Info = CommandInfo.Value;
				if (!Info.IsAdmin)
				{
					OutputProc("You are not authorized to use that command!");
					return ExitCode.BadCommand;
				}
				if (!Info.IsAdminChannel)
				{
					OutputProc("Use this command in an admin channel!");
					return ExitCode.BadCommand;
				}
			}
			return base.DoRun(parameters);
		}
	}
}
