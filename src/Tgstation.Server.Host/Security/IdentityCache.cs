using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class IdentityCache : IIdentityCache, IDisposable
	{
		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="IdentityCache"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="IdentityCache"/>.
		/// </summary>
		readonly ILogger<IdentityCache> logger;

		/// <summary>
		/// The map of <see cref="Api.Models.EntityId.Id"/>s to <see cref="IdentityCacheObject"/>s.
		/// </summary>
		readonly Dictionary<long, IdentityCacheObject> cachedIdentities;

		/// <summary>
		/// Initializes a new instance of the <see cref="IdentityCache"/> class.
		/// </summary>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public IdentityCache(IAsyncDelayer asyncDelayer, ILogger<IdentityCache> logger)
		{
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			cachedIdentities = new Dictionary<long, IdentityCacheObject>();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			logger.LogTrace("Disposing...");
			foreach (var cachedIdentity in cachedIdentities.Select(x => x.Value).ToList())
				cachedIdentity.Dispose();
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

				if (cachedIdentities.TryGetValue(user.Id.Value, out var identCache))
				{
					logger.LogTrace("Expiring previously cached identity...");
					identCache.Dispose(); // also clears it out
				}

				identCache = new IdentityCacheObject(
					systemIdentity.Clone(),
					asyncDelayer,
					() =>
					{
						logger.LogDebug("Expiring system identity cache for user {1}", uid, user.Id);
						lock (cachedIdentities)
							cachedIdentities.Remove(user.Id.Value);
					},
					expiry);
				cachedIdentities.Add(user.Id.Value, identCache);
			}
		}

		/// <inheritdoc />
		public ISystemIdentity LoadCachedIdentity(User user)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			lock (cachedIdentities)
				if (cachedIdentities.TryGetValue(user.Id.Value, out var identity))
					return identity.SystemIdentity.Clone();
			throw new InvalidOperationException("Cached system identity has expired!");
		}
	}
}
