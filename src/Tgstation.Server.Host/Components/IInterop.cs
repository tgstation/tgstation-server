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
		event EventHandler<ServerControlEventArgs> OnServerControl;
		event EventHandler<ChatMessageEventArgs> OnChatMessage;

		Version MinimumApiVersion { get; }
		Version MaximumApiVersion { get; }

		Task<Version> GetApiVersion(CancellationToken cancellationToken);

		void SetActivePort(ushort? port);

		Task<string> ChatCommand(string command, string arguments, CancellationToken cancellationToken);

		Task<ChatResponse> PromptChatResponse(EventType eventType, CancellationToken cancellationToken);
    }
}
