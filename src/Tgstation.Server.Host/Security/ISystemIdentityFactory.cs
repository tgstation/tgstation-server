using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Factory for <see cref="ISystemIdentity"/>s.
	/// </summary>
	public interface ISystemIdentityFactory
	{
		/// <summary>
		/// Retrieves a <see cref="ISystemIdentity"/> representing the user executing tgstation-server.
		/// </summary>
		/// <returns>A <see cref="ISystemIdentity"/> representing the user executing tgstation-server.</returns>
		ISystemIdentity GetCurrent();

		/// <summary>
		/// Create a <see cref="ISystemIdentity"/> for a given <paramref name="user"/>.
		/// </summary>
		/// <param name="user">The user to create a <see cref="ISystemIdentity"/> for.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A new <see cref="ISystemIdentity"/> or <see langword="null"/> if the <paramref name="user"/> has no <see cref="ISystemIdentity"/>.</returns>
		Task<ISystemIdentity> CreateSystemIdentity(User user, CancellationToken cancellationToken);

		/// <summary>
		/// Create a <see cref="ISystemIdentity"/> for a given username and password.
		/// </summary>
		/// <param name="username">The username of the user.</param>
		/// <param name="password">The password of the user.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A new <see cref="ISystemIdentity"/>.</returns>
		Task<ISystemIdentity> CreateSystemIdentity(string username, string password, CancellationToken cancellationToken);
	}
}
