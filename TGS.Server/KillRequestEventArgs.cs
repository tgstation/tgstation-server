using System;

namespace TGS.Server
{
	/// <summary>
	/// Event args for a kill request from DreamDaemon
	/// </summary>
	sealed class KillRequestEventArgs : EventArgs
	{
		/// <summary>
		/// If a chat message should be sent if and when DreamDaemon is restarted
		/// </summary>
		public bool SilentReboot => silentReboot;

		/// <summary>
		/// Backing field for <see cref="SilentReboot"/>
		/// </summary>
		readonly bool silentReboot;

		/// <summary>
		/// Construct a <see cref="KillRequestEventArgs"/>
		/// </summary>
		/// <param name="_silentReboot">The value of <see cref="SilentReboot"/></param>
		public KillRequestEventArgs(bool _silentReboot)
		{
			silentReboot = _silentReboot;
		}
	}
}
