using System;
using System.Collections.Generic;
using TGS.Interface.Components;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	interface IInteropManager : ITGInterop
	{
		/// <summary>
		/// Called when a request to terminate the world is recieved
		/// </summary>
		event EventHandler OnKillRequest;

		/// <summary>
		/// Called from /world/Reboot()
		/// </summary>
		event EventHandler OnWorldReboot;

		/// <summary>
		/// The local port used for calling /world/Topic()
		/// </summary>
		ushort TopicPort { set; }

		/// <summary>
		/// Resets the known DMAPI version. The world will have to resend it's API verification in order to use interop
		/// </summary>
		void ResetDMAPIVersion();

		/// <summary>
		/// Copies the DreamDaemon bridge dll from the program directory to the <see cref="ITGInstance"/> directory
		/// </summary>
		/// <param name="overwrite">If <see langword="true"/>, overwrites the <see cref="Instance"/>'s current bridge .dll if it exists</param>
		void UpdateBridgeDll(bool overwrite);

		/// <summary>
		/// Formats the string for BYOND's "-param" command line parameter to set up interop
		/// </summary>
		/// <returns></returns>
		string StartParameters();

		/// <summary>
		/// Set the communications key for server /world/Topic() calls
		/// </summary>
		/// <param name="newKey">The value for the communications key. If <see langword="null"/>, generates a random string for it</param>
		void SetCommunicationsKey(string newKey = null);

		/// <summary>
		/// Send a <paramref name="command"/> to /world/Topic()
		/// </summary>
		/// <param name="command">The <see cref="InteropCommand"/> to send</param>
		/// <param name="parameters">Parameters for the command</param>
		/// <returns>The result of /world/Topic() from running the command</returns>
		string SendCommand(InteropCommand command, IEnumerable<string> parameters = null);
	}
}
