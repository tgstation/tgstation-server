using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc cref="IAuthenticationContext" />
	sealed class AuthenticationContextFactory : IAuthenticationContextFactory, IDisposable
	{
		/// <summary>
		/// The <see cref="IAuthenticationContext"/> the <see cref="AuthenticationContextFactory"/> created.
		/// </summary>
		public IAuthenticationContext CurrentAuthenticationContext => currentAuthenticationContext;

		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="AuthenticationContextFactory"/>.
		/// </summary>
		readonly IDatabaseContext databaseContext;

		/// <summary>
		/// The <see cref="IIdentityCache"/> for the <see cref="AuthenticationContextFactory"/>.
		/// </summary>
		readonly IIdentityCache identityCache;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="AuthenticationContextFactory"/>.
		/// </summary>
		readonly ILogger<AuthenticationContextFactory> logger;

		/// <summary>
		/// The <see cref="SwarmConfiguration"/> for the <see cref="AuthenticationContextFactory"/>.
		/// </summary>
		readonly SwarmConfiguration swarmConfiguration;

		/// <summary>
		/// Backing field for <see cref="CurrentAuthenticationContext"/>.
		/// </summary>
		readonly AuthenticationContext currentAuthenticationContext;

		/// <summary>
		/// The <see cref="DateTimeOffset"/> the request's token must be valid after.
		/// </summary>
		DateTimeOffset? validAfter;

		/// <summary>
		/// 1 if <see cref="currentAuthenticationContext"/> was initialized, 0 otherwise.
		/// </summary>
		int initialized;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationContextFactory"/> class.
		/// </summary>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/>.</param>
		/// <param name="identityCache">The value of <see cref="identityCache"/>.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="swarmConfiguration"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public AuthenticationContextFactory(
			IDatabaseContext databaseContext,
			IIdentityCache identityCache,
			IOptions<SwarmConfiguration> swarmConfigurationOptions,
			ILogger<AuthenticationContextFactory> logger)
		{
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			this.identityCache = identityCache ?? throw new ArgumentNullException(nameof(identityCache));
			swarmConfiguration = swarmConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			currentAuthenticationContext = new AuthenticationContext();
		}

		/// <inheritdoc />
		public void Dispose() => currentAuthenticationContext.Dispose();

		/// <summary>
		/// Populate <see cref="validAfter"/> with a given <paramref name="tokenNbf"/>.
		/// </summary>
		/// <param name="tokenNbf">The <see cref="DateTimeOffset"/> an issued token is not valid before.</param>
		public void SetTokenNbf(DateTimeOffset tokenNbf)
		{
			if (validAfter.HasValue)
				throw new InvalidOperationException("SetTokenNbf called multiple times!");

			validAfter = tokenNbf;
		}

		/// <inheritdoc />
		public async ValueTask<IAuthenticationContext> CreateAuthenticationContext(long userId, long? instanceId, CancellationToken cancellationToken)
		{
			if (Interlocked.Exchange(ref initialized, 1) != 0)
				throw new InvalidOperationException("Authentication context has already been loaded");

			if (!validAfter.HasValue)
				throw new InvalidOperationException("SetTokenNbf has not been called!");

			var user = await databaseContext
				.Users
				.AsQueryable()
				.Where(x => x.Id == userId)
				.Include(x => x.CreatedBy)
				.Include(x => x.PermissionSet)
				.Include(x => x.Group)
					.ThenInclude(x => x.PermissionSet)
				.Include(x => x.OAuthConnections)
				.FirstOrDefaultAsync(cancellationToken);
			if (user == default)
			{
				logger.LogWarning("Unable to find user with ID {userId}!", userId);
				return currentAuthenticationContext;
			}

			ISystemIdentity systemIdentity;
			if (user.SystemIdentifier != null)
				systemIdentity = identityCache.LoadCachedIdentity(user);
			else
			{
				if (user.LastPasswordUpdate.HasValue && user.LastPasswordUpdate > validAfter.Value)
				{
					logger.LogDebug("Rejecting token for user {userId} created before last password update: {lastPasswordUpdate}", userId, user.LastPasswordUpdate.Value);
					return currentAuthenticationContext;
				}

				systemIdentity = null;
			}

			var userPermissionSet = user.PermissionSet ?? user.Group.PermissionSet;
			try
			{
				InstancePermissionSet instancePermissionSet = null;
				if (instanceId.HasValue)
				{
					instancePermissionSet = await databaseContext.InstancePermissionSets
						.AsQueryable()
						.Where(x => x.PermissionSetId == userPermissionSet.Id && x.InstanceId == instanceId && x.Instance.SwarmIdentifer == swarmConfiguration.Identifier)
						.Include(x => x.Instance)
						.FirstOrDefaultAsync(cancellationToken);

					if (instancePermissionSet == null)
						logger.LogDebug("User {userId} does not have permissions on instance {instanceId}!", userId, instanceId.Value);
				}

				currentAuthenticationContext.Initialize(
					systemIdentity,
					user,
					instancePermissionSet);
				return currentAuthenticationContext;
			}
			catch
			{
				systemIdentity?.Dispose();
				throw;
			}
		}
	}
}
