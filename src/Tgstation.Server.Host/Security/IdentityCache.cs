using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc cref="IIdentityCache" />
	sealed class IdentityCache : IIdentityCache, IAsyncDisposable
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
		public ValueTask DisposeAsync()
		{
			logger.LogTrace("Disposing...");
			return ValueTaskExtensions.WhenAll(cachedIdentities.Select(x => x.Value.DisposeAsync()));
		}

		/// <inheritdoc />
		public async ValueTask CacheSystemIdentity(User user, ISystemIdentity systemIdentity, DateTimeOffset expiry)
		{
			ArgumentNullException.ThrowIfNull(user);
			ArgumentNullException.ThrowIfNull(systemIdentity);

			var uid = user.Require(x => x.Id);
			var sysId = systemIdentity.Uid;

			ValueTask oldIdentityDisposal = ValueTask.CompletedTask;
			try
			{
				lock (cachedIdentities)
				{
					logger.LogDebug("Caching system identity {sysId} of user {uid}", sysId, uid);

					if (cachedIdentities.TryGetValue(uid, out var identCache))
					{
						logger.LogTrace("Expiring previously cached identity...");
						oldIdentityDisposal = identCache.DisposeAsync(); // also clears it out
					}

					identCache = new IdentityCacheObject(
						systemIdentity.Clone(),
						asyncDelayer,
						() =>
						{
							logger.LogDebug("Expiring system identity cache for user {uid}", uid);
							lock (cachedIdentities)
								cachedIdentities.Remove(uid);
						},
						expiry);
					cachedIdentities.Add(uid, identCache);
				}
			}
			finally
			{
				await oldIdentityDisposal;
			}
		}

		/// <inheritdoc />
		public ISystemIdentity LoadCachedIdentity(User user)
		{
			ArgumentNullException.ThrowIfNull(user);
			var uid = user.Require(x => x.Id);
			lock (cachedIdentities)
				if (cachedIdentities.TryGetValue(uid, out var identity))
					return identity.SystemIdentity.Clone();

			throw new InvalidOperationException("Cached system identity has expired!");
		}
	}
}
