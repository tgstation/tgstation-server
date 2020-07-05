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
		UserAgent = 1,

		/// <summary>
		/// <see cref="Microsoft.Net.Http.Headers.HeaderNames.Accept"/> header.
		/// </summary>
		Accept = 2,

		/// <summary>
		/// Api header.
		/// </summary>
		Api = 4,

		/// <summary>
		/// <see cref="Microsoft.Net.Http.Headers.HeaderNames.Authorization"/>
		/// </summary>
		Authorization = 8
	}
}