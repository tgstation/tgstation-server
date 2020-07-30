using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class AuthenticationContextFactory : IAuthenticationContextFactory, IDisposable
	{
		/// <inheritdoc />
		public IAuthenticationContext CurrentAuthenticationContext { get; private set; }

		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="AuthenticationContextFactory"/>
		/// </summary>
		readonly IDatabaseContext databaseContext;

		/// <summary>
		/// The <see cref="IIdentityCache"/> for the <see cref="AuthenticationContextFactory"/>
		/// </summary>
		readonly IIdentityCache identityCache;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="AuthenticationContextFactory"/>.
		/// </summary>
		readonly ILogger<AuthenticationContextFactory> logger;

		/// <summary>
		/// Construct an <see cref="AuthenticationContextFactory"/>
		/// </summary>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/></param>
		/// <param name="identityCache">The value of <see cref="identityCache"/></param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public AuthenticationContextFactory(
			IDatabaseContext databaseContext,
			IIdentityCache identityCache,
			ILogger<AuthenticationContextFactory> logger)
		{
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			this.identityCache = identityCache ?? throw new ArgumentNullException(nameof(identityCache));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public void Dispose() => CurrentAuthenticationContext?.Dispose();

		/// <inheritdoc />
		public async Task CreateAuthenticationContext(long userId, long? instanceId, DateTimeOffset validAfter, CancellationToken cancellationToken)
		{
			if (CurrentAuthenticationContext != null)
				throw new InvalidOperationException("Authentication context has already been loaded");

			var user = await databaseContext
				.Users
				.AsQueryable()
				.Where(x => x.Id == userId)
				.Include(x => x.CreatedBy)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);
			if (user == default)
			{
				logger.LogWarning("Unable to find user with ID {0}!", userId);
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
					logger.LogDebug("Rejecting token for user {0} created before last password update: {1}", userId, user.LastPasswordUpdate.Value);
					CurrentAuthenticationContext = new AuthenticationContext();
					return;
				}

				systemIdentity = null;
			}

			try
			{
				InstanceUser instanceUser = null;
				if (instanceId.HasValue)
				{
					instanceUser = await databaseContext.InstanceUsers
						.AsQueryable()
						.Where(x => x.UserId == userId && x.InstanceId == instanceId)
						.Include(x => x.Instance)
						.FirstOrDefaultAsync(cancellationToken)
						.ConfigureAwait(false);

					if (instanceUser == null)
						logger.LogDebug("User {0} does not have permissions on instance {1}!", userId, instanceId.Value);
				}

				CurrentAuthenticationContext = new AuthenticationContext(systemIdentity, user, instanceUser);
			}
			catch
			{
				systemIdentity?.Dispose();
				throw;
			}
		}
	}
}
