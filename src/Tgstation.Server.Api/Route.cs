using System.Net.Http;

namespace Tgstation.Server.Api
{
	/// <summary>
	/// Represents a route to a server action
	/// </summary>
	public sealed class Route
	{
		/// <summary>
		/// The path to the action
		/// </summary>
		public string Path { get; set; }

		/// <summary>
		/// The method of the action
		/// </summary>
		public HttpMethod Method { get; set; }
	}
}
