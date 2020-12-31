using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Configuration;
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
		/// The <see cref="SwarmConfiguration"/> for the <see cref="AuthenticationContextFactory"/>.
		/// </summary>
		readonly SwarmConfiguration swarmConfiguration;

		/// <summary>
		/// Construct an <see cref="AuthenticationContextFactory"/>
		/// </summary>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/></param>
		/// <param name="identityCache">The value of <see cref="identityCache"/></param>
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
				.Include(x => x.PermissionSet)
				.Include(x => x.Group)
					.ThenInclude(x => x.PermissionSet)
				.Include(x => x.OAuthConnections)
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
						.FirstOrDefaultAsync(cancellationToken)
						.ConfigureAwait(false);

					if (instancePermissionSet == null)
						logger.LogDebug("User {0} does not have permissions on instance {1}!", userId, instanceId.Value);
				}

				CurrentAuthenticationContext = new AuthenticationContext(
					systemIdentity,
					user,
					instancePermissionSet);
			}
			catch
			{
				systemIdentity?.Dispose();
				throw;
			}
		}
	}
}
