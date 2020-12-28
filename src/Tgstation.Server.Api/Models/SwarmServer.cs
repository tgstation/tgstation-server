namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class SwarmServer : Internal.SwarmServer
	{
		/// <summary>
		/// If the <see cref="SwarmServer"/> is the controller.
		/// </summary>
		public bool Controller { get; set; }
	}
}
