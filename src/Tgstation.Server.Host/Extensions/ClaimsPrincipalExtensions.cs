using System;
using System.Globalization;
using System.Security.Claims;

using Microsoft.IdentityModel.JsonWebTokens;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for the <see cref="ClaimsPrincipal"/> class.
	/// </summary>
	static class ClaimsPrincipalExtensions
	{
		/// <summary>
		/// Parse the <see cref="Models.User"/> <see cref="Api.Models.EntityId.Id"/> out of a given <paramref name="principal"/>.
		/// </summary>
		/// <param name="principal">The <see cref="ClaimsPrincipal"/> to use to parse the user ID.</param>
		/// <returns>The user ID in the <paramref name="principal"/>.</returns>
		public static long GetTgsUserId(this ClaimsPrincipal principal)
		{
			ArgumentNullException.ThrowIfNull(principal);

			var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
			if (userIdClaim == default)
				throw new InvalidOperationException($"Missing '{JwtRegisteredClaimNames.Sub}' claim!");

			long userId;
			try
			{
				userId = Int64.Parse(userIdClaim.Value, CultureInfo.InvariantCulture);
			}
			catch (Exception e)
			{
				throw new InvalidOperationException("Failed to parse user ID!", e);
			}

			return userId;
		}
	}
}
