﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc cref="IAuthenticationContext" />
	sealed class AuthenticationContextFactory : ITokenValidator, IDisposable
	{
		/// <summary>
		/// Internal scheme prefix for OIDC schemes.
		/// </summary>
		public const string OpenIDConnectAuthenticationSchemePrefix = $"{OpenIdConnectDefaults.AuthenticationScheme}.";

		/// <summary>
		/// Claim name used to set user groups in OIDC strict mode.
		/// </summary>
		public const string TgsGroupIdClaimName = "tgstation-server-group-id";

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
		/// The <see cref="SecurityConfiguration"/> for the <see cref="AuthenticationContextFactory"/>.
		/// </summary>
		readonly SecurityConfiguration securityConfiguration;

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
		/// Parse a <see cref="DateTimeOffset"/> out of a <see cref="Claim"/> in a given <paramref name="principal"/>.
		/// </summary>
		/// <param name="principal">The <see cref="ClaimsPrincipal"/> containing claims.</param>
		/// <param name="key">The <see cref="Claim"/> name to parse from.</param>
		/// <returns>The parsed <see cref="DateTimeOffset"/>.</returns>
		static DateTimeOffset ParseTime(ClaimsPrincipal principal, string key)
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

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationContextFactory"/> class.
		/// </summary>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/>.</param>
		/// <param name="identityCache">The value of <see cref="identityCache"/>.</param>
		/// <param name="apiHeadersProvider">The <see cref="IApiHeadersProvider"/> containing the value of <see cref="apiHeaders"/>.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="swarmConfiguration"/>.</param>
		/// <param name="securityConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="securityConfiguration"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public AuthenticationContextFactory(
			IDatabaseContext databaseContext,
			IIdentityCache identityCache,
			IApiHeadersProvider apiHeadersProvider,
			IOptions<SwarmConfiguration> swarmConfigurationOptions,
			IOptions<SecurityConfiguration> securityConfigurationOptions,
			ILogger<AuthenticationContextFactory> logger)
		{
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			this.identityCache = identityCache ?? throw new ArgumentNullException(nameof(identityCache));
			ArgumentNullException.ThrowIfNull(apiHeadersProvider);

			apiHeaders = apiHeadersProvider.ApiHeaders;
			swarmConfiguration = swarmConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions));
			securityConfiguration = securityConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(securityConfigurationOptions));

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			currentAuthenticationContext = new AuthenticationContext();
		}

		/// <inheritdoc />
		public void Dispose() => currentAuthenticationContext.Dispose();

		/// <inheritdoc />
		#pragma warning disable CA1506 // TODO: Decomplexify
		public async Task ValidateTgsToken(Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext tokenValidatedContext, CancellationToken cancellationToken)
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

			var notBefore = ParseTime(principal, JwtRegisteredClaimNames.Nbf);
			var expires = ParseTime(principal, JwtRegisteredClaimNames.Exp);

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

		/// <inheritdoc />
		public async Task ValidateOidcToken(RemoteAuthenticationContext<OpenIdConnectOptions> tokenValidatedContext, string schemeKey, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(tokenValidatedContext);

			var principal = tokenValidatedContext.Principal;
			if (principal == null)
				throw new InvalidOperationException("Expected a valid principal here!");

			var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
			if (userIdClaim == default)
				throw new InvalidOperationException($"Missing '{JwtRegisteredClaimNames.Sub}' claim!");

			var userId = userIdClaim.Value;
			var scheme = tokenValidatedContext.Scheme.Name;
			var deprefixedScheme = scheme.Substring(OpenIDConnectAuthenticationSchemePrefix.Length);
			var connection = await databaseContext
				.OidcConnections
				.AsQueryable()
				.Where(oidcConnection => oidcConnection.ExternalUserId == userId && oidcConnection.SchemeKey == deprefixedScheme)
				.Include(oidcConnection => oidcConnection.User)
					.ThenInclude(user => user!.Group)
						.ThenInclude(group => group!.PermissionSet)
				.FirstOrDefaultAsync(cancellationToken);

			User user;
			if (!securityConfiguration.OidcStrictMode)
			{
				if (connection == default)
				{
					tokenValidatedContext.Fail($"Unable to find user with OidcConnection for {deprefixedScheme}/{userId}!");
					return;
				}

				user = connection.User!;
			}
			else
			{
				var groupClaim = principal.FindFirst(TgsGroupIdClaimName);
				long? groupId;
				if (groupClaim == default)
					groupId = null;
				else if (Int64.TryParse(groupClaim.Value, out long groupIdParsed))
					groupId = groupIdParsed;
				else
				{
					tokenValidatedContext.Fail($"User has non-numeric '{TgsGroupIdClaimName}' claim!");
					return;
				}

				UserGroup? group = groupId.HasValue
					? await databaseContext
						.Groups
						.AsQueryable()
						.Where(group => group.Id == groupId.Value)
						.Include(group => group.PermissionSet)
						.FirstOrDefaultAsync(cancellationToken)
					: null;

				var missingClaimError = $"User missing '{TgsGroupIdClaimName}' claim!";
				if (connection == default)
				{
					var username = principal.Identity?.Name;
					if (username == null)
					{
						tokenValidatedContext.Fail("Failed to retrieve user's name from retrieved claims!");
						return;
					}

					if (username.Contains(':'))
					{
						tokenValidatedContext.Fail("Cannot create users with the ':' in their name!");
						return;
					}

					if (group == null)
					{
						tokenValidatedContext.Fail(
							groupId.HasValue
								? $"'{TgsGroupIdClaimName}' does not point to a valid group!"
								: missingClaimError);
						return;
					}

					var tgsUser = await databaseContext
						.Users
						.GetTgsUser(
							dbUser => new User
							{
								Id = dbUser.Id!.Value,
							},
							cancellationToken);

					user = new User
					{
						CreatedAt = DateTimeOffset.UtcNow,
						CanonicalName = User.CanonicalizeName(username),
						Name = username,
						CreatedById = tgsUser.Id,
						Enabled = true,
						GroupId = group.Id,
						OidcConnections = new List<OidcConnection>
						{
							new OidcConnection
							{
								SchemeKey = schemeKey,
								ExternalUserId = userId,
							},
						},
						PasswordHash = "_", // This can't be hashed
					};

					databaseContext.Users.Add(user);
				}
				else
				{
					user = connection.User!;

					// group update
					if (group == null)
					{
						user.PermissionSet = new PermissionSet
						{
							AdministrationRights = AdministrationRights.None,
							InstanceManagerRights = InstanceManagerRights.None,
						};
						user.GroupId = null;
						user.Enabled = false;

						tokenValidatedContext.Fail(missingClaimError);
						return;
					}

					user.Group = group;
					if (user.PermissionSet != null)
						databaseContext.PermissionSets.Remove(user.PermissionSet);

					user.Enabled = true;
				}

				await databaseContext.Save(cancellationToken);
			}

			var expires = ParseTime(principal, JwtRegisteredClaimNames.Exp);

			currentAuthenticationContext.Initialize(
				user,
				expires,
				Guid.NewGuid().ToString(),
				null,
				null);
		}
	}
}
