using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using Tgstation.Server.Api;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc cref="IAuthenticationContext" />
	sealed class AuthenticationContextFactory : ITokenValidator, IDisposable
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
		/// The <see cref="ApiHeaders"/> for the <see cref="AuthenticationContextFactory"/>.
		/// </summary>
		readonly ApiHeaders? apiHeaders;

		/// <summary>
		/// 1 if <see cref="currentAuthenticationContext"/> was initialized, 0 otherwise.
		/// </summary>
		int initialized;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationContextFactory"/> class.
		/// </summary>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/>.</param>
		/// <param name="identityCache">The value of <see cref="identityCache"/>.</param>
		/// <param name="apiHeadersProvider">The <see cref="IApiHeadersProvider"/> containing the value of <see cref="apiHeaders"/>.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="swarmConfiguration"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public AuthenticationContextFactory(
			IDatabaseContext databaseContext,
			IIdentityCache identityCache,
			IApiHeadersProvider apiHeadersProvider,
			IOptions<SwarmConfiguration> swarmConfigurationOptions,
			ILogger<AuthenticationContextFactory> logger)
		{
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			this.identityCache = identityCache ?? throw new ArgumentNullException(nameof(identityCache));
			ArgumentNullException.ThrowIfNull(apiHeadersProvider);

			apiHeaders = apiHeadersProvider.ApiHeaders;
			swarmConfiguration = swarmConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			currentAuthenticationContext = new AuthenticationContext();
		}

		/// <inheritdoc />
		public void Dispose() => currentAuthenticationContext.Dispose();

		/// <inheritdoc />
		#pragma warning disable CA1506 // TODO: Decomplexify
		public async Task ValidateToken(TokenValidatedContext tokenValidatedContext, CancellationToken cancellationToken)
		#pragma warning restore CA1506
		{
			ArgumentNullException.ThrowIfNull(tokenValidatedContext);

			if (tokenValidatedContext.SecurityToken is not JsonWebToken jwt)
				throw new ArgumentException($"Expected {nameof(tokenValidatedContext)} to contain a {nameof(JsonWebToken)}!", nameof(tokenValidatedContext));

			if (Interlocked.Exchange(ref initialized, 1) != 0)
				throw new InvalidOperationException("Authentication context has already been loaded");

			var principal = new ClaimsPrincipal(new ClaimsIdentity(jwt.Claims));

			var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
			if (userIdClaim == default)
				throw new InvalidOperationException($"Missing '{JwtRegisteredClaimNames.Sub}' claim!");

			long userId;
			try
			{
				userId = Int64.Parse(userIdClaim.Value, CultureInfo.InvariantCulture);
			}
			catch (Exception e)
			{
				throw new InvalidOperationException("Failed to parse user ID!", e);
			}

			DateTimeOffset ParseTime(string key)
			{
				var claim = principal.FindFirst(key);
				if (claim == default)
					throw new InvalidOperationException($"Missing '{key}' claim!");

				try
				{
					return new DateTimeOffset(
						EpochTime.DateTime(
							Int64.Parse(claim.Value, CultureInfo.InvariantCulture)));
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException($"Failed to parse '{key}'!", ex);
				}
			}

			var notBefore = ParseTime(JwtRegisteredClaimNames.Nbf);
			var expires = ParseTime(JwtRegisteredClaimNames.Exp);

			var user = await databaseContext
				.Users
				.AsQueryable()
				.Where(x => x.Id == userId)
				.Include(x => x.CreatedBy)
				.Include(x => x.PermissionSet)
				.Include(x => x.Group)
					.ThenInclude(x => x!.PermissionSet)
				.Include(x => x.OAuthConnections)
				.FirstOrDefaultAsync(cancellationToken);
			if (user == default)
			{
				tokenValidatedContext.Fail($"Unable to find user with ID {userId}!");
				return;
			}

			ISystemIdentity? systemIdentity;
			if (user.SystemIdentifier != null)
				systemIdentity = identityCache.LoadCachedIdentity(user);
			else
			{
				if (user.LastPasswordUpdate.HasValue && user.LastPasswordUpdate >= notBefore)
				{
					tokenValidatedContext.Fail($"Rejecting token for user {userId} created before last modification: {user.LastPasswordUpdate.Value}");
					return;
				}

				systemIdentity = null;
			}

			var userPermissionSet = user.PermissionSet ?? user.Group!.PermissionSet;
			try
			{
				InstancePermissionSet? instancePermissionSet = null;
				var instanceId = apiHeaders?.InstanceId;
				if (instanceId.HasValue)
				{
					instancePermissionSet = await databaseContext.InstancePermissionSets
						.AsQueryable()
						.Where(x => x.PermissionSetId == userPermissionSet!.Id && x.InstanceId == instanceId && x.Instance!.SwarmIdentifer == swarmConfiguration.Identifier)
						.Include(x => x.Instance)
						.FirstOrDefaultAsync(cancellationToken);

					if (instancePermissionSet == null)
						logger.LogDebug("User {userId} does not have permissions on instance {instanceId}!", userId, instanceId.Value);
				}

				currentAuthenticationContext.Initialize(
					user,
					expires,
					jwt.EncodedSignature, // signature is enough to uniquely identify the session as it is composite of all the inputs
					instancePermissionSet,
					systemIdentity);
			}
			catch
			{
				systemIdentity?.Dispose();
				throw;
			}
		}
	}
}
