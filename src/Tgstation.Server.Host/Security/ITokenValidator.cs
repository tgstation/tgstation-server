using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Handles validating authentication tokens.
	/// </summary>
	public interface ITokenValidator
	{
		/// <summary>
		/// Handles TGS <paramref name="tokenValidatedContext"/>s.
		/// </summary>
		/// <param name="tokenValidatedContext">The <see cref="Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task ValidateTgsToken(Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext tokenValidatedContext, CancellationToken cancellationToken);

		/// <summary>
		/// Handles OIDC <paramref name="tokenValidatedContext"/>s.
		/// </summary>
		/// <param name="tokenValidatedContext">The <see cref="RemoteAuthenticationContext{TOptions}"/> for <see cref="OpenIdConnectOptions"/>.</param>
		/// <param name="schemeKey">The scheme key being used to login.</param>
		/// <param name="groupIdClaimName">The name of the <see cref="global::System.Security.Claims.Claim"/> used to set the user's group ID.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task ValidateOidcToken(RemoteAuthenticationContext<OpenIdConnectOptions> tokenValidatedContext, string schemeKey, string groupIdClaimName, CancellationToken cancellationToken);
	}
}
