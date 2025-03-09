using System.Threading;
using System.Threading.Tasks;

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
		/// <param name="tokenValidatedContext">The <see cref="Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task ValidateOidcToken(Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext tokenValidatedContext, CancellationToken cancellationToken);
	}
}
