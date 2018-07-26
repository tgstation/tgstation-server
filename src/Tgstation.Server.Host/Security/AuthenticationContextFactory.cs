using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class AuthenticationContextFactory : IAuthenticationContextFactory, IDisposable
	{
		/// <inheritdoc />
		public IAuthenticationContext CurrentAuthenticationContext { get; private set; }

		/// <summary>
		/// The <see cref="ISystemIdentityFactory"/> for the <see cref="AuthenticationContextFactory"/>
		/// </summary>
		readonly ISystemIdentityFactory systemIdentityFactory;

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="AuthenticationContextFactory"/>
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// Map of <see cref="Api.Models.Internal.User.Id"/>s to <see cref="IdentityCache"/>s for that user
		/// </summary>
		readonly Dictionary<long, IdentityCache> identityCache;

		/// <summary>
		/// Construct an <see cref="AuthenticationContextFactory"/>
		/// </summary>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/></param>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/></param>
		public AuthenticationContextFactory(ISystemIdentityFactory systemIdentityFactory, IDatabaseContextFactory databaseContextFactory)
		{
			this.systemIdentityFactory = systemIdentityFactory ?? throw new ArgumentNullException(nameof(systemIdentityFactory));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));

			identityCache = new Dictionary<long, IdentityCache>();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			foreach (var I in identityCache)
				I.Value.Dispose();
		}

		/// <inheritdoc />
		public void CacheSystemIdentity(User user, ISystemIdentity systemIdentity, DateTimeOffset expiry)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			if (systemIdentity == null)
				throw new ArgumentNullException(nameof(systemIdentity));
			lock (identityCache)
			{
				if (identityCache.TryGetValue(user.Id, out var identCache))
					identCache.Dispose();   //also clears it out
				identCache = new IdentityCache(systemIdentity.Clone(), expiry, () =>
				{
					lock (identityCache)
						identityCache.Remove(user.Id);
				});
				identityCache.Add(user.Id, identCache);
			}
		}

		/// <inheritdoc />
		public async Task CreateAuthenticationContext(long userId, long? instanceId, CancellationToken cancellationToken)
		{
			if (CurrentAuthenticationContext != null)
				throw new InvalidOperationException("Authentication context has already been loaded");

			User user = null;
			await databaseContextFactory.UseContext(async db =>
			{
				var userQuery = db.Users.Where(x => x.Id == userId);

				if (instanceId.HasValue)
					userQuery = userQuery.Include(x => x.InstanceUsers.Where(y => y.Id == instanceId));

				user = await userQuery.Include(x => x.InstanceUsers).FirstAsync(cancellationToken).ConfigureAwait(false);
			}).ConfigureAwait(false);

			InstanceUser instanceUser = null;
			if (instanceId.HasValue)
				instanceUser = user.InstanceUsers.Where(x => x.InstanceId == instanceId).FirstOrDefault();

			if (instanceUser == default)
				return;

			ISystemIdentity systemIdentity;
			if (user.SystemIdentifier != null)
				lock (identityCache)
				{
					if (!identityCache.TryGetValue(userId, out var identCache))
						throw new InvalidOperationException("Cached system identity has expired!");
					systemIdentity = identCache.SystemIdentity.Clone();
				}
			else
				systemIdentity = null;

			CurrentAuthenticationContext = new AuthenticationContext(systemIdentity, user, instanceUser);
		}
	}
}
