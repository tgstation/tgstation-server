namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// An <see cref="Api"/> access token
	/// </summary>
	public sealed class Token : TokenInfo
	{
		/// <summary>
		/// The token <see cref="string"/>
		/// </summary>
		public string Value { get; set; }
	}
}