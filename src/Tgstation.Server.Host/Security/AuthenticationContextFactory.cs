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
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			this.identityCache = identityCache ?? throw new ArgumentNullException(nameof(identityCache));
		}

		/// <inheritdoc />
		public async Task CreateAuthenticationContext(long userId, long? instanceId, DateTimeOffset validAfter, CancellationToken cancellationToken)
		{
			if (CurrentAuthenticationContext != null)
				throw new InvalidOperationException("Authentication context has already been loaded");

			var userQuery = databaseContext.Users.Where(x => x.Id == userId)
				.Include(x => x.CreatedBy)
				.FirstOrDefaultAsync(cancellationToken);

			var instanceUserQuery = instanceId.HasValue ? databaseContext.InstanceUsers
				.Where(x => x.UserId == userId && x.InstanceId == instanceId && x.Instance.Online.Value)
				.Include(x => x.Instance)
				.FirstOrDefaultAsync(cancellationToken) : Task.FromResult<InstanceUser>(null);

			var user = await userQuery.ConfigureAwait(false);
			if (user == default)
			{
				CurrentAuthenticationContext = new AuthenticationContext();
				return;
			}

			ISystemIdentity systemIdentity;
			if (user.SystemIdentifier != null)
				systemIdentity = identityCache.LoadCachedIdentity(user);
			else
			{
				if (user.LastPasswordUpdate.HasValue && user.LastPasswordUpdate > validAfter)
				{
					CurrentAuthenticationContext = new AuthenticationContext();
					return;
				}
				systemIdentity = null;
			}

			var instanceUser = await instanceUserQuery.ConfigureAwait(false);

			CurrentAuthenticationContext = new AuthenticationContext(systemIdentity, user, instanceUser);
		}
	}
}
