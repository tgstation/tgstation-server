using System.Security.Claims;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Interface for accessing the current request's <see cref="ClaimsPrincipal"/>.
	/// </summary>
	interface IClaimsPrincipalAccessor
	{
		/// <summary>
		/// Get the current <see cref="ClaimsPrincipal"/>.
		/// </summary>
		ClaimsPrincipal User { get; }
	}
}
