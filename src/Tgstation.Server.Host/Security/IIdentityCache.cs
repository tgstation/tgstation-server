using System;

using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// For caching <see cref="ISystemIdentity"/>s.
	/// </summary>
	public interface IIdentityCache
	{
		/// <summary>
		/// Keep a <paramref name="user"/>'s <paramref name="systemIdentity"/> alive until an <paramref name="expiry"/> time.
		/// </summary>
		/// <param name="user">The <see cref="User"/> the <paramref name="systemIdentity"/> belongs to.</param>
		/// <param name="systemIdentity">The <see cref="ISystemIdentity"/> to cache.</param>
		/// <param name="expiry">When the <paramref name="systemIdentity"/> should expire.</param>
		void CacheSystemIdentity(User user, ISystemIdentity systemIdentity, DateTimeOffset expiry);

		/// <summary>
		/// Attempt to load a cached <see cref="ISystemIdentity"/>.
		/// </summary>
		/// <param name="user">The <see cref="User"/> the <see cref="ISystemIdentity"/> belongs to.</param>
		/// <returns>The cached <see cref="ISystemIdentity"/>.</returns>
		ISystemIdentity LoadCachedIdentity(User user);
	}
}
