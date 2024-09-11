namespace Tgstation.Server.Host.Authority.Core
{
	/// <summary>
	/// Indicates the type of HTTP status code an failing <see cref="AuthorityResponse"/> should generate.
	/// </summary>
	public enum HttpFailureResponse
	{
		/// <summary>
		/// HTTP 400.
		/// </summary>
		BadRequest,

		/// <summary>
		/// HTTP 401.
		/// </summary>
		Unauthorized,

		/// <summary>
		/// HTTP 403.
		/// </summary>
		Forbidden,

		/// <summary>
		/// HTTP 404.
		/// </summary>
		NotFound,

		/// <summary>
		/// HTTP 406.
		/// </summary>
		NotAcceptable,

		/// <summary>
		/// HTTP 409.
		/// </summary>
		Conflict,

		/// <summary>
		/// HTTP 410.
		/// </summary>
		Gone,

		/// <summary>
		/// HTTP 422.
		/// </summary>
		UnprocessableEntity,

		/// <summary>
		/// HTTP 424.
		/// </summary>
		FailedDependency,

		/// <summary>
		/// HTTP 429.
		/// </summary>
		RateLimited,

		/// <summary>
		/// HTTP 501.
		/// </summary>
		NotImplemented,
	}
}
