using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.GraphQL.Mutations.Payloads;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Security.OAuth;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Authority
{
	/// <inheritdoc cref="ILoginAuthority" />
	sealed class LoginAuthority : AuthorityBase, ILoginAuthority
	{
		/// <summary>
		/// The <see cref="IApiHeadersProvider"/> for the <see cref="LoginAuthority"/>.
		/// </summary>
		readonly IApiHeadersProvider apiHeadersProvider;

		/// <summary>
		/// The <see cref="ISystemIdentityFactory"/> for the <see cref="LoginAuthority"/>.
		/// </summary>
		readonly ISystemIdentityFactory systemIdentityFactory;

		/// <summary>
		/// The <see cref="IOAuthProviders"/> for the <see cref="LoginAuthority"/>.
		/// </summary>
		readonly IOAuthProviders oAuthProviders;

		/// <summary>
		/// The <see cref="ITokenFactory"/> for the <see cref="LoginAuthority"/>.
		/// </summary>
		readonly ITokenFactory tokenFactory;

		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="LoginAuthority"/>.
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// The <see cref="IIdentityCache"/> for the <see cref="LoginAuthority"/>.
		/// </summary>
		readonly IIdentityCache identityCache;

		/// <summary>
		/// The <see cref="ISessionInvalidationTracker"/> for the <see cref="LoginAuthority"/>.
		/// </summary>
		readonly ISessionInvalidationTracker sessionInvalidationTracker;

		/// <summary>
		/// The <see cref="IOptionsSnapshot{TOptions}"/> containing the <see cref="SecurityConfiguration"/> for the <see cref="LoginAuthority"/>.
		/// </summary>
		readonly IOptionsSnapshot<SecurityConfiguration> securityConfigurationOptions;

		/// <summary>
		/// Generate an <see cref="AuthorityResponse{TResult}"/> for a given <paramref name="headersException"/>.
		/// </summary>
		/// <typeparam name="TResult">The <see cref="Type"/> of <see cref="AuthorityResponse{TResult}"/> to generate.</typeparam>
		/// <param name="headersException">The <see cref="HeadersException"/> to generate a response for.</param>
		/// <returns>A new, errored <see cref="LoginResult"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		static AuthorityResponse<TResult> GenerateHeadersExceptionResponse<TResult>(HeadersException headersException)
			=> new(
				new ErrorMessageResponse(ErrorCode.BadHeaders)
				{
					AdditionalData = headersException.Message,
				},
				headersException.ParseErrors.HasFlag(HeaderErrorTypes.Accept)
					? HttpFailureResponse.NotAcceptable
					: HttpFailureResponse.BadRequest);

		/// <summary>
		/// Select the details needed to generate a <see cref="TokenResponse"/> from a given <paramref name="query"/>.
		/// </summary>
		/// <param name="query">The <see cref="IQueryable{T}"/> of <see cref="User"/>s.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="User"/> returned after selecting, if any.</returns>
		static async ValueTask<User?> SelectUserInfoFromQuery(IQueryable<User> query, CancellationToken cancellationToken)
		{
			var users = await query
				.ToListAsync(cancellationToken);

			// Pick the DB user first
			var user = users
				.OrderByDescending(dbUser => dbUser.SystemIdentifier == null)
				.FirstOrDefault();

			return user;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LoginAuthority"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to use.</param>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		/// <param name="apiHeadersProvider">The value of <see cref="apiHeadersProvider"/>.</param>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/>.</param>
		/// <param name="oAuthProviders">The value of <see cref="oAuthProviders"/>.</param>
		/// <param name="tokenFactory">The value of <see cref="tokenFactory"/>.</param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/>.</param>
		/// <param name="identityCache">The value of <see cref="identityCache"/>.</param>
		/// <param name="sessionInvalidationTracker">The value of <see cref="sessionInvalidationTracker"/>.</param>
		/// <param name="securityConfigurationOptions">The value of <see cref="securityConfigurationOptions"/>.</param>
		public LoginAuthority(
			IDatabaseContext databaseContext,
			ILogger<LoginAuthority> logger,
			IApiHeadersProvider apiHeadersProvider,
			ISystemIdentityFactory systemIdentityFactory,
			IOAuthProviders oAuthProviders,
			ITokenFactory tokenFactory,
			ICryptographySuite cryptographySuite,
			IIdentityCache identityCache,
			ISessionInvalidationTracker sessionInvalidationTracker,
			IOptionsSnapshot<SecurityConfiguration> securityConfigurationOptions)
			: base(
				  databaseContext,
				  logger)
		{
			this.apiHeadersProvider = apiHeadersProvider ?? throw new ArgumentNullException(nameof(apiHeadersProvider));
			this.systemIdentityFactory = systemIdentityFactory ?? throw new ArgumentNullException(nameof(systemIdentityFactory));
			this.oAuthProviders = oAuthProviders ?? throw new ArgumentNullException(nameof(oAuthProviders));
			this.tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.identityCache = identityCache ?? throw new ArgumentNullException(nameof(identityCache));
			this.sessionInvalidationTracker = sessionInvalidationTracker ?? throw new ArgumentNullException(nameof(sessionInvalidationTracker));
			this.securityConfigurationOptions = securityConfigurationOptions ?? throw new ArgumentNullException(nameof(securityConfigurationOptions));
		}

		/// <inheritdoc />
		public RequirementsGated<AuthorityResponse<GeneratedToken>> AttemptLogin(CancellationToken cancellationToken)
			=> new(
				() => null,
				() => AttemptLoginImpl(cancellationToken),
				doNotAddUserSessionValidRequirement: true);

		/// <inheritdoc />
		public RequirementsGated<AuthorityResponse<OAuthGatewayLoginResult>> AttemptOAuthGatewayLogin(CancellationToken cancellationToken)
			=> new(
				() => (IAuthorizationRequirement?)null,
				async () =>
				{
					var headers = apiHeadersProvider.ApiHeaders;
					if (headers == null)
						return GenerateHeadersExceptionResponse<OAuthGatewayLoginResult>(apiHeadersProvider.HeadersException!);

					var oAuthProvider = headers.OAuthProvider;
					if (!oAuthProvider.HasValue)
						return BadRequest<OAuthGatewayLoginResult>(ErrorCode.BadHeaders);

					var (errorResponse, oAuthResult) = await TryOAuthenticate<OAuthGatewayLoginResult>(headers, oAuthProvider.Value, false, cancellationToken);
					if (errorResponse != null)
						return errorResponse;

					Logger.LogDebug("Generated {provider} OAuth AccessCode", oAuthProvider.Value);

					return new(
						new OAuthGatewayLoginResult
						{
							AccessCode = oAuthResult!.Value.AccessCode,
						});
				});

		/// <summary>
		/// Login process.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="AuthorityResponse{TResult}"/> containing the <see cref="GeneratedToken"/>.</returns>
		private async ValueTask<AuthorityResponse<GeneratedToken>> AttemptLoginImpl(CancellationToken cancellationToken)
		{
			// password and oauth logins disabled
			if (securityConfigurationOptions.Value.OidcStrictMode)
				return Unauthorized<GeneratedToken>();

			var headers = apiHeadersProvider.ApiHeaders;
			if (headers == null)
				return GenerateHeadersExceptionResponse<GeneratedToken>(apiHeadersProvider.HeadersException!);

			if (headers.IsTokenAuthentication)
				return BadRequest<GeneratedToken>(ErrorCode.TokenWithToken);

			var oAuthLogin = headers.OAuthProvider.HasValue;

			ISystemIdentity? systemIdentity = null;
			if (!oAuthLogin)
				try
				{
					// trust the system over the database because a user's name can change while still having the same SID
					systemIdentity = await systemIdentityFactory.CreateSystemIdentity(headers.Username!, headers.Password!, cancellationToken);
				}
				catch (NotImplementedException)
				{
					// Intentionally suppressed
				}

			using (systemIdentity)
			{
				// Get the user from the database
				IQueryable<User> query = DatabaseContext.Users;
				if (oAuthLogin)
				{
					var oAuthProvider = headers.OAuthProvider!.Value;
					var (errorResponse, oauthResult) = await TryOAuthenticate<GeneratedToken>(headers, oAuthProvider, true, cancellationToken);
					if (errorResponse != null)
						return errorResponse;

					query = query.Where(
						x => x.OAuthConnections!.Any(
							y => y.Provider == oAuthProvider
							&& y.ExternalUserId == oauthResult!.Value.UserID));
				}
				else
				{
					var canonicalUserName = User.CanonicalizeName(headers.Username!);
					if (canonicalUserName == User.CanonicalizeName(User.TgsSystemUserName))
						return Unauthorized<GeneratedToken>();

					if (systemIdentity == null)
						query = query.Where(x => x.CanonicalName == canonicalUserName);
					else
						query = query.Where(x => x.CanonicalName == canonicalUserName || x.SystemIdentifier == systemIdentity.Uid);
				}

				var user = await SelectUserInfoFromQuery(query, cancellationToken);

				// No user? You're not allowed
				if (user == null)
					return Unauthorized<GeneratedToken>();

				// A system user may have had their name AND password changed to one in our DB...
				// Or a DB user was created that had the same user/pass as a system user
				// Dumb admins...
				// FALLBACK TO THE DB USER HERE, DO NOT REVEAL A SYSTEM LOGIN!!!
				// This of course, allows system users to discover TGS users in this (HIGHLY IMPROBABLE) case but that is not our fault
				var originalHash = user.PasswordHash;
				var isLikelyDbUser = originalHash != null;
				var usingSystemIdentity = systemIdentity != null && !isLikelyDbUser;
				if (!oAuthLogin)
					if (!usingSystemIdentity)
					{
						// DB User password check and update
						if (!isLikelyDbUser || !cryptographySuite.CheckUserPassword(user, headers.Password!))
							return Unauthorized<GeneratedToken>();
						if (user.PasswordHash != originalHash)
						{
							Logger.LogDebug("User ID {userId}'s password hash needs a refresh, updating database.", user.Id);
							var updatedUser = new User
							{
								Id = user.Require(x => x.Id),
							};
							DatabaseContext.Users.Attach(updatedUser);
							updatedUser.PasswordHash = user.PasswordHash;
							await DatabaseContext.Save(cancellationToken);
						}
					}
					else
					{
						var usernameMismatch = systemIdentity!.Username != user.Name;
						if (isLikelyDbUser || usernameMismatch)
						{
							DatabaseContext.Users.Attach(user);
							if (usernameMismatch)
							{
								// System identity username change update
								Logger.LogDebug("User ID {userId}'s system identity needs a refresh, updating database.", user.Id);
								user.Name = systemIdentity.Username;
								user.CanonicalName = User.CanonicalizeName(user.Name);
							}

							if (isLikelyDbUser)
							{
								// cleanup from https://github.com/tgstation/tgstation-server/issues/1528
								Logger.LogDebug("System user ID {userId}'s PasswordHash is polluted, updating database.", user.Id);
								user.PasswordHash = null;
								sessionInvalidationTracker.UserModifiedInvalidateSessions(user);
							}

							await DatabaseContext.Save(cancellationToken);
						}
					}

				// Now that the bookeeping is done, tell them to fuck off if necessary
				if (!user.Enabled!.Value)
				{
					Logger.LogTrace("Not logging in disabled user {userId}.", user.Id);
					return Forbid<GeneratedToken>();
				}

				var token = tokenFactory.CreateToken(user, oAuthLogin);

				if (usingSystemIdentity)
					await CacheSystemIdentity(systemIdentity!, user, token.Expiry);

				Logger.LogDebug("Successfully logged in user {userId}!", user.Id);

				return new AuthorityResponse<GeneratedToken>(token);
			}
		}

		/// <summary>
		/// Add a given <paramref name="systemIdentity"/> to the <see cref="identityCache"/>.
		/// </summary>
		/// <param name="systemIdentity">The <see cref="ISystemIdentity"/> to cache.</param>
		/// <param name="user">The <see cref="User"/> the <paramref name="systemIdentity"/> was generated for.</param>
		/// <param name="validTo">When the user's session exipres.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		private async ValueTask CacheSystemIdentity(ISystemIdentity systemIdentity, User user, DateTimeOffset validTo)
		{
			// expire the identity slightly after the auth token in case of lag
			var identExpiry = validTo;
			identExpiry += tokenFactory.ValidationParameters.ClockSkew;
			identExpiry += TimeSpan.FromSeconds(15);
			await identityCache.CacheSystemIdentity(user, systemIdentity!, identExpiry);
		}

		/// <summary>
		/// Attempt OAuth authentication.
		/// </summary>
		/// <typeparam name="TResult">The <see cref="Type"/> to use for errored <see cref="AuthorityResponse{TResult}"/>s.</typeparam>
		/// <param name="headers">The current <see cref="ApiHeaders"/>.</param>
		/// <param name="oAuthProvider">The <see cref="OAuthProvider"/> to use.</param>
		/// <param name="forLogin">If this is for a server login.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in an errored <see cref="AuthorityResponse{TResult}"/> on failure or the result of the call to <see cref="IOAuthValidator.ValidateResponseCode(string, bool, CancellationToken)"/> on success.</returns>
		async ValueTask<(AuthorityResponse<TResult>? ErrorResponse, (string? UserID, string AccessCode)? OAuthResult)> TryOAuthenticate<TResult>(ApiHeaders headers, OAuthProvider oAuthProvider, bool forLogin, CancellationToken cancellationToken)
		{
			(string? UserID, string AccessCode)? oauthResult;
			try
			{
				// minor special case here until its removal
#pragma warning disable CS0618 // Type or member is obsolete
				if (oAuthProvider == OAuthProvider.TGForums)
#pragma warning restore CS0618 // Type or member is obsolete
					return (Unauthorized<TResult>(), null);

				var validator = oAuthProviders
					.GetValidator(oAuthProvider, forLogin);

				if (validator == null)
					return (BadRequest<TResult>(ErrorCode.OAuthProviderDisabled), null);
				oauthResult = await validator
					.ValidateResponseCode(headers.OAuthCode!, forLogin, cancellationToken);

				Logger.LogTrace("External {oAuthProvider} UID: {externalUserId}", oAuthProvider, oauthResult);
			}
			catch (Octokit.RateLimitExceededException ex)
			{
				return (RateLimit<TResult>(ex), null);
			}

			if (!oauthResult.HasValue)
				return (Unauthorized<TResult>(), null);

			return (null, OAuthResult: oauthResult);
		}
	}
}
