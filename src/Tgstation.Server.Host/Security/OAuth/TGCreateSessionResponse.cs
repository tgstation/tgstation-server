namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// Response when creating a tgstation forums session.
	/// </summary>
	sealed class TGCreateSessionResponse : TGBaseResponse
	{
		/// <summary>
		/// The session's private token. Similar to OAuth authorization response code.
		/// </summary>
		public string SessionPrivateToken { get; set; }

		/// <summary>
		/// The session's public token. Barely similar to OAuth client ID.
		/// </summary>
		public string SessionPublicToken { get; set; }
	}
}
