namespace Tgstation.Server.Host.GraphQL.Subscriptions
{
	/// <summary>
	/// Reasons TGS may invalidate a user's login session.
	/// </summary>
	public enum SessionInvalidationReason
	{
		/// <summary>
		/// The callers JWT expired.
		/// </summary>
		TokenExpired,

		/// <summary>
		/// An update to the caller's identity requiring reauthentication was made.
		/// </summary>
		UserUpdated,

		/// <summary>
		/// TGS is shutting down or restarting. Note, depending on server configuration, the current session may not actually be invalid upon restarting. However, the information required to determine this is not exposed to clients.
		/// </summary>
		ServerShutdown,
	}
}
