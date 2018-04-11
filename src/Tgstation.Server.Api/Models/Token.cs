namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a JWT returned by the API
	/// </summary>
	public sealed class Token
	{
		/// <summary>
		/// The value of the JWT
		/// </summary>
		public string Value { get; set; }
	}
}
