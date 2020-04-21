namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	public sealed class BridgeResponse
	{
		public string ErrorMessage { get; set; }
		public ushort? NewPort { get; set; }
	}
}
