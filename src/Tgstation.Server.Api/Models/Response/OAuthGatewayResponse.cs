namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Success result for an OAuth gateway login attempt.
	/// </summary>
	public sealed class OAuthGatewayResponse
	{
		/// <summary>
		/// The user's access token for the requested OAuth service.
		/// </summary>
		public string? AccessCode { get; set; }
	}
}
