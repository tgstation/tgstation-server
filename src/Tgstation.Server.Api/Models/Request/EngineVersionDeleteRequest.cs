namespace Tgstation.Server.Api.Models.Request
{
	/// <summary>
	/// A request to delete a specific <see cref="EngineVersion"/>.
	/// </summary>
	public class EngineVersionDeleteRequest
	{
		/// <summary>
		/// The <see cref="Models.EngineVersion"/> to delete.
		/// </summary>
		public EngineVersion? EngineVersion { get; set; }
	}
}
