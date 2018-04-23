using System;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Represents a chat broadcast request
	/// </summary>
	sealed class ChatMessageEventArgs : EventArgs
	{
		/// <summary>
		/// The <see cref="ChatResponse"/> to send out
		/// </summary>
		public ChatResponse ChatResponse { get; set; }
	}
}