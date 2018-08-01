using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class AuthenticationContextFactory : IAuthenticationContextFactory
	{
		/// <inheritdoc />
		public IAuthenticationContext CurrentAuthenticationContext { get; private set; }

		/// <summary>
		/// The <see cref="ISystemIdentityFactory"/> for the <see cref="AuthenticationContextFactory"/>
		/// </summary>
		readonly ISystemIdentityFactory systemIdentityFactory;

		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="AuthenticationContextFactory"/>
		/// </summary>
		readonly IDatabaseContext databaseContext;

		/// <summary>
		/// The <see cref="IIdentityCache"/> for the <see cref="AuthenticationContextFactory"/>
		/// </summary>
		readonly IIdentityCache identityCache;

		/// <summary>
		/// Construct an <see cref="AuthenticationContextFactory"/>
		/// </summary>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/></param>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/></param>
		/// <param name="identityCache">The value of <see cref="identityCache"/></param>
		public AuthenticationContextFactory(ISystemIdentityFactory systemIdentityFactory, IDatabaseContext databaseContext, IIdentityCache identityCache)
		{
			this.systemIdentityFactory = systemIdentityFactory ?? throw new ArgumentNullException(nameof(systemIdentityFactory));
			this.databaseContext = databaseContext?? throw new ArgumentNullException(nameof(databaseContext));
			this.identityCache = identityCache ?? throw new ArgumentNullException(nameof(identityCache));
		}

		/// <inheritdoc />
		public async Task CreateAuthenticationContext(long userId, long? instanceId, CancellationToken cancellationToken)
		{
			if (CurrentAuthenticationContext != null)
				throw new InvalidOperationException("Authentication context has already been loaded");

			var userQuery = databaseContext.Users.Where(x => x.Id == userId).FirstAsync(cancellationToken);

			var instanceUser = instanceId.HasValue ? (await databaseContext.InstanceUsers
				.Where(x => x.UserId == userId)
				.Where(x => x.InstanceId == instanceId)
				.Include(x => x.Instance)
				.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)) : null;

			var user = await userQuery.ConfigureAwait(false);

			ISystemIdentity systemIdentity;
			if (user.SystemIdentifier != null)
			{
				systemIdentity = identityCache.LoadCachedIdentity(user);
				if (systemIdentity == null)
					throw new InvalidOperationException("Cached system identity has expired!");
			}
			else
				systemIdentity = null;

			CurrentAuthenticationContext = new AuthenticationContext(systemIdentity, user, instanceUser);
		}
	}
}
