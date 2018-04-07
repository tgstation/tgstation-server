using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Core
{
	interface ITokenManager
	{
		Task<User> GetUser(string token, CancellationToken cancellationToken);
		Task<Token> CreateToken(User user, CancellationToken cancellationToken);
		Task<IReadOnlyList<Token>> UserTokens(User user, CancellationToken cancellationToken);
		Task<IDictionary<User, Token>> AllTokens(CancellationToken cancellationToken);
	}
}
