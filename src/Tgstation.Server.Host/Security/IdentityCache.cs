using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class IdentityCache : IIdentityCache, IDisposable
	{
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="IdentityCache"/>
		/// </summary>
		readonly ILogger<IdentityCache> logger;

		/// <summary>
		/// The map of <see cref="Api.Models.Internal.User.Id"/>s to <see cref="IdentityCacheObject"/>s
		/// </summary>
		readonly Dictionary<long, IdentityCacheObject> cachedIdentities;

		/// <summary>
		/// Construct an <see cref="IdentityCache"/>
		/// </summary>
		public IdentityCache(ILogger<IdentityCache> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			cachedIdentities = new Dictionary<long, IdentityCacheObject>();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			logger.LogTrace("Disposing...");
			foreach (var I in cachedIdentities.Select(x => x.Value).ToList())
				I.Dispose();
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
				var uid = systemIdentity.Uid;
				logger.LogDebug("Caching system identity {0} of user {1}", uid, user.Id);

				if (cachedIdentities.TryGetValue(user.Id, out var identCache))
				{
					logger.LogTrace("Expiring previously cached identity...");
					identCache.Dispose();   //also clears it out
				}
				identCache = new IdentityCacheObject(systemIdentity.Clone(), () =>
				{
					logger.LogDebug("Expiring system identity cache for user {1}", uid, user.Id);
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
				if (cachedIdentities.TryGetValue(user.Id, out var identity))
					return identity.SystemIdentity.Clone();
			throw new InvalidOperationException("Cached system identity has expired!");
		}
	}
}
