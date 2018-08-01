using System;
using System.Collections.Generic;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class IdentityCache : IIdentityCache, IDisposable
	{
		readonly Dictionary<long, IdentityCacheObject> cachedIdentities;

		public IdentityCache()
		{
			cachedIdentities = new Dictionary<long, IdentityCacheObject>();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			foreach (var I in cachedIdentities)
				I.Value.Dispose();
		}

		/// <inheritdoc />
		public void CacheSystemIdentity(User user, ISystemIdentity systemIdentity, DateTimeOffset expiry)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			if (systemIdentity == null)
				throw new ArgumentNullException(nameof(systemIdentity));
			lock (cachedIdentities)
			{
				if (cachedIdentities.TryGetValue(user.Id, out var identCache))
					identCache.Dispose();   //also clears it out
				identCache = new IdentityCacheObject(systemIdentity.Clone(), () =>
				{
					lock (cachedIdentities)
						cachedIdentities.Remove(user.Id);
				}, expiry);
				cachedIdentities.Add(user.Id, identCache);
			}
		}

		/// <inheritdoc />
		public ISystemIdentity LoadCachedIdentity(User user)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			lock (cachedIdentities)
			{
				if (cachedIdentities.TryGetValue(user.Id, out var identity))
					return identity.SystemIdentity;
				return null;
			}
		}
	}
}
