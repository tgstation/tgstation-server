using System;
using TGS.Interface.Components;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	interface IDreamDaemonManager : ITGDreamDaemon
	{
		/// <summary>
		/// Runs some code while DreamDaemon is suspended
		/// </summary>
		/// <param name="action">The code to run</param>
		void RunSuspended(Action action);
	}
}
