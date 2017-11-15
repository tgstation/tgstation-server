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
		/// The communications key for server Topic calls. If set to null, will be regenerated
		/// </summary>
		string CommunicationsKey { get; set; }

		/// <summary>
		/// Send a <paramref name="command"/> over interop
		/// </summary>
		/// <param name="command">The <see cref="InteropCommand"/> to send</param>
		/// <param name="parameters">Parameters for the command</param>
		void SendCommand(InteropCommand command, IDictionary<string, string> parameters = null);
	}
}
