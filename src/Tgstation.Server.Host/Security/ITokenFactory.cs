using Microsoft.IdentityModel.Tokens;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// For creating <see cref="Token"/>s
	/// </summary>
	public interface ITokenFactory
	{
		/// <summary>
		/// The <see cref="TokenValidationParameters"/> for the <see cref="ITokenFactory"/>
		/// </summary>
		TokenValidationParameters ValidationParameters { get; }

		/// <summary>
		/// Create a <see cref="Token"/> for a given <paramref name="user"/>
		/// </summary>
		/// <param name="user">The <see cref="Models.User"/> to create the token for. Must have the <see cref="Api.Models.Internal.User.Id"/> field available</param>
		/// <param name="oAuth">Whether or not this is an OAuth login.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a new <see cref="Token"/></returns>
		Task<Token> CreateToken(Models.User user, bool oAuth, CancellationToken cancellationToken);
	}
}
