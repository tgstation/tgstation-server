using System;

namespace Tgstation.Server.Api
{
	/// <summary>
	/// Types of individual <see cref="ApiHeaders"/>.
	/// </summary>
	[Flags]
	public enum HeaderTypes
	{
		/// <summary>
		/// No headers.
		/// </summary>
		None = 0,

		/// <summary>
		/// <see cref="Microsoft.Net.Http.Headers.HeaderNames.UserAgent"/> header.
		/// </summary>
		UserAgent = 1 << 0,

		/// <summary>
		/// <see cref="Microsoft.Net.Http.Headers.HeaderNames.Accept"/> header.
		/// </summary>
		Accept = 1 << 1,

		/// <summary>
		/// <see cref="ApiHeaders.ApiVersionHeader"/>.
		/// </summary>
		Api = 1 << 2,

		/// <summary>
		/// <see cref="Microsoft.Net.Http.Headers.HeaderNames.Authorization"/>
		/// </summary>
		Authorization = 1 << 3,

		/// <summary>
		/// <see cref="ApiHeaders.OAuthProviderHeader"/>.
		/// </summary>
		OAuthProvider = 1 << 4,
	}
}
