using System;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class ClaimsPrincipalAccessor : IClaimsPrincipalAccessor
	{
		/// <inheritdoc />
		public ClaimsPrincipal User => httpContextAccessor.HttpContext?.User
			?? throw new InvalidOperationException("HTTP context was not present!");

		/// <summary>
		/// The <see cref="IHttpContextAccessor"/> for the <see cref="AuthorizationService"/>.
		/// </summary>
		readonly IHttpContextAccessor httpContextAccessor;

		/// <summary>
		/// Initializes a new instance of the <see cref="ClaimsPrincipalAccessor"/> class.
		/// </summary>
		/// <param name="httpContextAccessor">The value of <see cref="httpContextAccessor"/>.</param>
		public ClaimsPrincipalAccessor(
			IHttpContextAccessor httpContextAccessor)
		{
			this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
		}
	}
}
