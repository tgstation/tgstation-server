using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Factory for <see cref="ISystemIdentity"/>s
	/// </summary>
	interface ISystemIdentityFactory
	{
		/// <summary>
		/// Create a <see cref="ISystemIdentity"/> for a given <paramref name="user"/>
		/// </summary>
		/// <param name="user">The user to create a <see cref="ISystemIdentity"/> for</param>
		/// <returns>A new <see cref="ISystemIdentity"/></returns>
		ISystemIdentity CreateSystemIdentity(User user);

		/// <summary>
		/// Create a <see cref="ISystemIdentity"/> for a given username and password
		/// </summary>
		/// <param name="username">The username of the user</param>
		/// <param name="password">The password of the user</param>
		/// <returns>A new <see cref="ISystemIdentity"/></returns>
		ISystemIdentity CreateSystemIdentity(string username, string password);
	}
}
