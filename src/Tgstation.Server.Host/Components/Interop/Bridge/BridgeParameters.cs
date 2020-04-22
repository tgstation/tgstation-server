using System;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	/// <summary>
	/// Parameters for a bridge request.
	/// </summary>
	public sealed class BridgeParameters : DMApiParameters
	{
		/// <summary>
		/// The <see cref="BridgeCommandType"/>.
		/// </summary>
		public BridgeCommandType? CommandType { get; set; }

		/// <summary>
		/// The current port for <see cref="BridgeCommandType.PortUpdate"/> requests.
		/// </summary>
		public ushort? CurrentPort { get; set; }

		/// <summary>
		/// The DMAPI <see cref="global::System.Version"/> for <see cref="BridgeCommandType.Validate"/> requests.
		/// </summary>
		public Version Version { get; set; }

		/// <summary>
		/// The minimum required <see cref="DreamDaemonSecurity"/> level for <see cref="BridgeCommandType.Validate"/> requests.
		/// </summary>
		public DreamDaemonSecurity? MinimumSecurityLevel { get; set; }

		/// <summary>
		/// The <see cref="Interop.ChatMessage"/> for <see cref="BridgeCommandType.ChatSend"/> requests.
		/// </summary>
		public ChatMessage ChatMessage { get; set; }
	}
}
