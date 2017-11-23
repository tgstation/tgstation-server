using System.Collections.Generic;
using System.Threading;
using TGS.Interface;

namespace TGS.Server.Chat.Commands
{
	/// <summary>
	/// A command heard by a <see cref="ChatProviders.IChatProvider"/>
	/// </summary>
	abstract class ChatCommand : Command
	{
		/// <summary>
		/// <see cref="CommandInfo"/> for the <see cref="ChatCommand"/>
		/// </summary>
		public static ThreadLocal<CommandInfo> ThreadCommandInfo { get; private set; } = new ThreadLocal<CommandInfo>();
		/// <summary>
		/// If set to <see langword="true"/>, the <see cref="ChatCommand"/> cannot be invoked by a non-admin or outside an admin chat channel
		/// </summary>
		public bool RequiresAdmin { get; protected set; }
		/// <summary>
		/// Shorthand for accessing <see cref="ThreadLocal{CommandInfo}.Value"/>
		/// </summary>
		protected CommandInfo CommandInfo { get { return ThreadCommandInfo.Value; } }

		/// <inheritdoc />
		public override ExitCode DoRun(IList<string> parameters)
		{
			if (RequiresAdmin)
			{
				if (!CommandInfo.IsAdmin)
				{
					OutputProc("You are not authorized to use that command!");
					return ExitCode.BadCommand;
				}
				if (!CommandInfo.IsAdminChannel)
				{
					OutputProc("Use this command in an admin channel!");
					return ExitCode.BadCommand;
				}
			}
			return base.DoRun(parameters);
		}
	}
}
