using System;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	public sealed class BridgeParameters : DMApiParameters
	{
		public BridgeCommandType? CommandType { get; set; }

		public ushort? NewPort { get; set; }

		public Version Version { get; set; }

		public ChatMessage ChatMessage { get; set; }

		public DreamDaemonSecurity? MinimumSecurityLevel { get; set; }
	}
}
