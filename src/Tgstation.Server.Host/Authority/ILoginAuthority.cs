using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Authority.Core;

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
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="TokenResponse"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		ValueTask<AuthorityResponse<TokenResponse>> AttemptLogin(CancellationToken cancellationToken);
	}
}
