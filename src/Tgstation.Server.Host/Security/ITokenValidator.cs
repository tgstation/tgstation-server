using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Handles <see cref="TokenValidatedContext"/>s.
	/// </summary>
	public interface ITokenValidator
	{
		/// <summary>
		/// Handles <paramref name="tokenValidatedContext"/>.
		/// </summary>
		/// <param name="tokenValidatedContext">The <see cref="TokenValidatedContext"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task ValidateToken(TokenValidatedContext tokenValidatedContext, CancellationToken cancellationToken);
	}
}
