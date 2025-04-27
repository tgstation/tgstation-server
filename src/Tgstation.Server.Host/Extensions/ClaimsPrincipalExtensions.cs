using System;
using System.Globalization;
using System.Security.Claims;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for the <see cref="ClaimsPrincipal"/> class.
	/// </summary>
	static class ClaimsPrincipalExtensions
	{
		/// <summary>
		/// Parse the <see cref="Models.User"/> <see cref="Api.Models.EntityId.Id"/> out of a given authenticated <paramref name="principal"/>.
		/// </summary>
		/// <param name="principal">The <see cref="ClaimsPrincipal"/> to use to parse the user ID.</param>
		/// <returns>The user ID in the <paramref name="principal"/> if it was present.</returns>
		public static long? GetTgsUserId(this ClaimsPrincipal principal)
		{
			ArgumentNullException.ThrowIfNull(principal);

			var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
			if (userIdClaim == default)
				return null;

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

		/// <summary>
		/// Parse the <see cref="Models.User"/> <see cref="Api.Models.EntityId.Id"/> out of a given authenticated <paramref name="principal"/>.
		/// </summary>
		/// <param name="principal">The <see cref="ClaimsPrincipal"/> to use to parse the user ID.</param>
		/// <returns>The user ID in the <paramref name="principal"/>.</returns>
		public static long RequireTgsUserId(this ClaimsPrincipal principal)
			=> principal.GetTgsUserId() ?? throw new InvalidOperationException($"Missing '{JwtRegisteredClaimNames.Sub}' claim!");

		/// <summary>
		/// Parse a <see cref="DateTimeOffset"/> out of a <see cref="Claim"/> in a given <paramref name="principal"/>.
		/// </summary>
		/// <param name="principal">The <see cref="ClaimsPrincipal"/> containing claims.</param>
		/// <param name="claimName">The <see cref="Claim"/> name to parse from.</param>
		/// <returns>The parsed <see cref="DateTimeOffset"/>.</returns>
		public static DateTimeOffset ParseTime(this ClaimsPrincipal principal, string claimName)
		{
			ArgumentNullException.ThrowIfNull(principal);
			ArgumentNullException.ThrowIfNull(claimName);

			var claim = principal.FindFirst(claimName);
			if (claim == null)
				throw new InvalidOperationException($"Missing '{claimName}' claim!");

			try
			{
				return new DateTimeOffset(
					EpochTime.DateTime(
						Int64.Parse(claim.Value, CultureInfo.InvariantCulture)));
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Failed to parse claim {claimName}: '{claim.Value}'!", ex);
			}
		}
	}
}
