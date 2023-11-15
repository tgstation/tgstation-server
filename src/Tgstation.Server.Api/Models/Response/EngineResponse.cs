namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents an installed <see cref="Models.EngineVersion"/>.
	/// </summary>
	public sealed class EngineResponse
	{
		/// <summary>
		/// The represented <see cref="Models.EngineVersion"/>. If <see langword="null"/> that indicates none were found.
		/// </summary>
		public EngineVersion? EngineVersion { get; set; }
	}
}
