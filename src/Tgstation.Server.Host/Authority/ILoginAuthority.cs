using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.GraphQL.Mutations.Payloads;

namespace Tgstation.Server.Host.Authority
{
	/// <summary>
	/// <see cref="IAuthority"/> for authenticating with the server.
	/// </summary>
	public interface ILoginAuthority : IAuthority
	{
		/// <summary>
		/// Attempt to login to the server with the current crentials.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="LoginPayload"/> and <see cref="Models.User"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		ValueTask<AuthorityResponse<LoginPayload>> AttemptLogin(CancellationToken cancellationToken);
	}
}
