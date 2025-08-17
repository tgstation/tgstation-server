using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.GraphQL.Mutations.Payloads;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Authority
{
	/// <summary>
	/// <see cref="IAuthority"/> for authenticating with the server.
	/// </summary>
	public interface ILoginAuthority : IAuthority
	{
		/// <summary>
		/// Attempt to login to the server with the current Basic or OAuth credentials.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="GeneratedToken"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		RequirementsGated<AuthorityResponse<GeneratedToken>> AttemptLogin(CancellationToken cancellationToken);

		/// <summary>
		/// Attempt to login to an OAuth service with the current OAuth credentials.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in an <see cref="OAuthGatewayLoginResult"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		RequirementsGated<AuthorityResponse<OAuthGatewayLoginResult>> AttemptOAuthGatewayLogin(CancellationToken cancellationToken);
	}
}
