namespace Tgstation.Server.Host.Authority.Core
{
	/// <summary>
	/// Indicates the type of HTTP status code a successful <see cref="AuthorityResponse{TResult}"/> should generate.
	/// </summary>
	public enum HttpSuccessResponse
	{
		/// <summary>
		/// HTTP 200.
		/// </summary>
		Ok,

		/// <summary>
		/// HTTP 201.
		/// </summary>
		Created,

		/// <summary>
		/// HTTP 202.
		/// </summary>
		Accepted,
	}
}
