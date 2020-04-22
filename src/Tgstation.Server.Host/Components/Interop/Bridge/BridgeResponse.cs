namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	/// <summary>
	/// A response to a bridge request.
	/// </summary>
	public sealed class BridgeResponse : DMApiResponse
	{
		/// <summary>
		/// The new port for <see cref="BridgeCommandType.Reboot"/> requests.
		/// </summary>
		public ushort? NewPort { get; set; }
	}
}
