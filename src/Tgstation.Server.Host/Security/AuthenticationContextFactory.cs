using Microsoft.AspNetCore.Http;
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
		/// Construct an <see cref="AuthenticationContextFactory"/>
		/// </summary>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/></param>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/></param>
		public AuthenticationContextFactory(ISystemIdentityFactory systemIdentityFactory, IDatabaseContext databaseContext)
		{
			this.systemIdentityFactory = systemIdentityFactory ?? throw new ArgumentNullException(nameof(systemIdentityFactory));
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
		}

		/// <inheritdoc />
		public async Task CreateAuthenticationContext(long userId, long? instanceId, CancellationToken cancellationToken)
		{
			if (CurrentAuthenticationContext != null)
				throw new InvalidOperationException("Authentication context has already been loaded");

			var userQuery = databaseContext.Users.Where(x => x.Id == userId);

			if (instanceId.HasValue)
				userQuery = userQuery.Include(x => x.InstanceUsers.Where(y => y.Id == instanceId));

			var user = await userQuery.FirstAsync(cancellationToken).ConfigureAwait(false);

			InstanceUser instanceUser = null;
			if (instanceId.HasValue)
				instanceUser = user.InstanceUsers.First();

			CurrentAuthenticationContext = new AuthenticationContext(systemIdentityFactory.CreateSystemIdentity(user), user, instanceUser);
		}
	}
}
