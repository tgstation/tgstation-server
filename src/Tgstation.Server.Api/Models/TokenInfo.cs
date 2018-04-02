using System.Net;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Information about a <see cref="Token"/>
	/// </summary>
	public class TokenInfo
	{
		/// <summary>
		/// The id of the <see cref="TokenInfo"/>
		/// </summary>
		public long Id { get; }

		/// <summary>
		/// The user agent that created the <see cref="TokenInfo"/>
		/// </summary>
		public string ClientUserAgent { get; set; }

		/// <summary>
		/// The <see cref="IPAddress"/> the <see cref="TokenInfo"/> was originally issued to
		/// </summary>
		public IPAddress IssuedTo { get; set; }
	}
}