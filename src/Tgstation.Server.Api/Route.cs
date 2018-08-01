using System;
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

		/// <summary>
		/// Adds a <paramref name="host"/> portion to <see cref="Path"/>
		/// </summary>
		/// <param name="host">The host address</param>
		/// <returns>A combined <paramref name="host"/> and <see cref="Path"/> <see cref="Uri"/></returns>
		public Uri Flatten(Uri host) => new Uri(String.Concat(host.ToString().TrimEnd('/'), Path));
	}
}
