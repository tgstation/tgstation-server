namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	/// <summary>
	/// Represents the <see cref="BridgeParameters.CommandType"/>.
	/// </summary>
	public enum BridgeCommandType
	{
		/// <summary>
		/// DreamDaemon notifying us of its current port and requesting a change if necessary.
		/// </summary>
		DeprecatedPortUpdate,

		/// <summary>
		/// DreamDaemon notifying it is starting.
		/// </summary>
		Startup,

		/// <summary>
		/// DreamDaemon notifying the server is primed.
		/// </summary>
		Prime,

		/// <summary>
		/// DreamDaemon notifiying the server is calling /world/Reboot().
		/// </summary>
		Reboot,

		/// <summary>
		/// DreamDaemon requesting the process be terminated.
		/// </summary>
		Kill,

		/// <summary>
		/// DreamDaemon requesting a <see cref="ChatMessage"/> be sent.
		/// </summary>
		ChatSend,

		/// <summary>
		/// DreamDaemon attempting to send a longer bridge message.
		/// </summary>
		Chunk,

		/// <summary>
		/// DreamDaemon requesting a custom event to be triggered.
		/// </summary>
		Event,

		/// <summary>
		/// DreamDaemon requesting TGS trigger a deployment.
		/// </summary>
		Deploy,
	}
}
