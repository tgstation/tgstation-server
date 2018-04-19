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
		/// Create a <see cref="Token"/> for a given <paramref name="user"/>
		/// </summary>
		/// <param name="user">The <see cref="Models.User"/> to create the token for. Must have the <see cref="Api.Models.Internal.User.Id"/> field available</param>
		/// <returns>A new <see cref="Token"/></returns>
		Token CreateToken(Models.User user);
	}
}
