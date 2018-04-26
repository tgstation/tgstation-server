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

		Version MinimumApiVersion { get; }
		Version MaximumApiVersion { get; }

		Task<Version> GetApiVersion(CancellationToken cancellationToken);

		void SetRun(ushort? port, string accessToken, bool primary);

		Task SwapAndActivateServers(CancellationToken cancellationToken);

		Task<string> ChatCommand(string command, string arguments, CancellationToken cancellationToken);

		Task<ChatResponse> PromptChatResponse(EventType eventType, CancellationToken cancellationToken);
    }
}
