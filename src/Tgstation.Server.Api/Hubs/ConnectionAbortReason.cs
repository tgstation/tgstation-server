namespace Tgstation.Server.Api.Hubs
{
	/// <summary>
	/// The reason an <see cref="IErrorHandlingHub"/> aborts a connection.
	/// </summary>
	public enum ConnectionAbortReason
	{
		/// <summary>
		/// The provided token is no longer authenticated or authorized to keep the connection.
		/// </summary>
		TokenInvalid,

		/// <summary>
		/// The server is restarting.
		/// </summary>
		ServerRestart,
	}
}
