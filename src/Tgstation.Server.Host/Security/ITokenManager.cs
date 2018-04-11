using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// For creating and decoding JWTs
	/// </summary>
	interface ITokenManager
	{
		/// <summary>
		/// Get the user associated with a <paramref name="token"/>
		/// </summary>
		/// <param name="token">The <see cref="Token"/> to get the user for</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>The <see cref="Models.User"/> associated with the <paramref name="token"/></returns>
		Task<Models.User> GetUser(Token token, CancellationToken cancellationToken);

		/// <summary>
		/// Create a <see cref="Token"/> for a given <paramref name="user"/>
		/// </summary>
		/// <param name="user">The <see cref="Models.User"/> to create the token for. Must have the <see cref="Api.Models.Internal.User.Id"/> and <see cref="Models.User.TokenSecret"/> fields available</param>
		/// <returns>A new <see cref="Token"/></returns>
		Token CreateToken(Models.User user);
	}
}
