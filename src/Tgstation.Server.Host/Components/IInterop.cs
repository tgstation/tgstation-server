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
		bool SecondaryIsOther { get; set; }

		void SetServerControlHandler(Func<ServerControlEvent, CancellationToken, Task> serverControlHandler);
		void SetChatMessageHandler(Func<ChatMessageEventArgs, CancellationToken, Task> chatMessageHandler);

		Task<Version> GetApiVersion(CancellationToken cancellationToken);

		void SetRun(ushort? port, string accessToken, bool primary);

		Task ActivateOtherServer(CancellationToken cancellationToken);

		Task<string> ChatCommand(string command, string arguments, CancellationToken cancellationToken);
    }
}
