using System;

namespace Tgstation.Server.Api
{
	/// <summary>
	/// Types of individual <see cref="ApiHeaders"/> errors.
	/// </summary>
	[Flags]
	public enum HeaderErrorTypes
	{
		/// <summary>
		/// No header errors.
		/// </summary>
		None = 0,

		/// <summary>
		/// The <see cref="Microsoft.Net.Http.Headers.HeaderNames.UserAgent"/> header is missing or invalid.
		/// </summary>
		UserAgent = 1 << 0,

		/// <summary>
		/// The <see cref="Microsoft.Net.Http.Headers.HeaderNames.Accept"/> header is missing or invalid.
		/// </summary>
		Accept = 1 << 1,

		/// <summary>
		/// The <see cref="ApiHeaders.ApiVersionHeader"/> header is missing or invalid.
		/// </summary>
		Api = 1 << 2,

		/// <summary>
		/// The <see cref="Microsoft.Net.Http.Headers.HeaderNames.Authorization"/> header is invalid.
		/// </summary>
		AuthorizationInvalid = 1 << 3,

		/// <summary>
		/// The <see cref="ApiHeaders.OAuthProviderHeader"/> header is missing or invalid.
		/// </summary>
		OAuthProvider = 1 << 4,

		/// <summary>
		/// The <see cref="Microsoft.Net.Http.Headers.HeaderNames.Authorization"/> header is missing.
		/// </summary>
		AuthorizationMissing = 1 << 5,
	}
}
