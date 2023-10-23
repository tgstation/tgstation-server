using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// For injecting <see cref="global::System.Security.Claims.Claim"/>s that <see cref="Security.TgsAuthorizeAttribute"/> can look for.
	/// </summary>
	interface IClaimsInjector
	{
		/// <summary>
		/// Setup the <see cref="global::System.Security.Claims.Claim"/>s for a given <paramref name="tokenValidatedContext"/>.
		/// </summary>
		/// <param name="tokenValidatedContext">The <see cref="TokenValidatedContext"/> containing the <see cref="Microsoft.AspNetCore.Http.HttpContext"/> and <see cref="Microsoft.IdentityModel.Tokens.SecurityToken"/> of the request and the <see cref="global::System.Security.Claims.ClaimsPrincipal"/> to add <see cref="global::System.Security.Claims.Claim"/>s to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task InjectClaimsIntoContext(TokenValidatedContext tokenValidatedContext, CancellationToken cancellationToken);
	}
}
