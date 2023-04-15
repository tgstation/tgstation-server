namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	/// <summary>
	/// A response to a bridge request.
	/// </summary>
	public class BridgeResponse : DMApiResponse
	{
		/// <summary>
		/// The new port for <see cref="BridgeCommandType.Reboot"/> requests.
		/// </summary>
		public ushort? NewPort { get; set; }

		/// <summary>
		/// The <see cref="Bridge.RuntimeInformation"/> for <see cref="BridgeCommandType.Startup"/> requests.
		/// </summary>
		public RuntimeInformation RuntimeInformation { get; set; }
	}
}
