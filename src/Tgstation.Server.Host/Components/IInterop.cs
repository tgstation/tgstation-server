using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Handles communication with the DMAPI
	/// </summary>
    interface IInterop
	{
		IInteropControl ReconnectToRun(string primaryAccessToken, string secondaryAccessToken, ushort primaryPort, ushort secondaryPort);

		IInteropControl CreateRun(ushort primaryPort, ushort? secondaryPort, Func<ChatMessageEventArgs, CancellationToken, Task> chatMessageHandler);
	}
}
