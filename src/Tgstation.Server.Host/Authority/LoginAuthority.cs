using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Database;
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
		/// The <see cref="IDatabaseContext"/> for the <see cref="LoginAuthority"/>.
		/// </summary>
		readonly IDatabaseContext databaseContext;

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
		/// Generate an <see cref="AuthorityResponse{TResult}"/> for a given <paramref name="headersException"/>.
		/// </summary>
		/// <param name="headersException">The <see cref="HeadersException"/> to generate a response for.</param>
		/// <returns>A new, errored <see cref="TokenResponse"/> <see cref="AuthorityResponse{TResult}"/>.</returns>
		static AuthorityResponse<TokenResponse> GenerateHeadersExceptionResponse(HeadersException headersException)
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
				.Select(x => new User
				{
					Id = x.Id,
					PasswordHash = x.PasswordHash,
					Enabled = x.Enabled,
					Name = x.Name,
					SystemIdentifier = x.SystemIdentifier,
				})
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
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> to use.</param>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		/// <param name="apiHeadersProvider">The value of <see cref="apiHeadersProvider"/>.</param>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/>.</param>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/>.</param>
		/// <param name="oAuthProviders">The value of <see cref="oAuthProviders"/>.</param>
		/// <param name="tokenFactory">The value of <see cref="tokenFactory"/>.</param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/>.</param>
		/// <param name="identityCache">The value of <see cref="identityCache"/>.</param>
		public LoginAuthority(
			IAuthenticationContext authenticationContext,
			ILogger<LoginAuthority> logger,
			IApiHeadersProvider apiHeadersProvider,
			ISystemIdentityFactory systemIdentityFactory,
			IDatabaseContext databaseContext,
			IOAuthProviders oAuthProviders,
			ITokenFactory tokenFactory,
			ICryptographySuite cryptographySuite,
			IIdentityCache identityCache)
			: base(authenticationContext, logger)
		{
			this.apiHeadersProvider = apiHeadersProvider ?? throw new ArgumentNullException(nameof(apiHeadersProvider));
			this.systemIdentityFactory = systemIdentityFactory ?? throw new ArgumentNullException(nameof(systemIdentityFactory));
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			this.oAuthProviders = oAuthProviders ?? throw new ArgumentNullException(nameof(oAuthProviders));
			this.tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.identityCache = identityCache ?? throw new ArgumentNullException(nameof(identityCache));
		}

		/// <inheritdoc />
		public async ValueTask<AuthorityResponse<TokenResponse>> AttemptLogin(CancellationToken cancellationToken)
		{
			var headers = apiHeadersProvider.ApiHeaders;
			if (headers == null)
				return GenerateHeadersExceptionResponse(apiHeadersProvider.HeadersException!);

			if (headers.IsTokenAuthentication)
				return BadRequest<TokenResponse>(ErrorCode.TokenWithToken);

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
				IQueryable<User> query = databaseContext.Users.AsQueryable();
				if (oAuthLogin)
				{
					var oAuthProvider = headers.OAuthProvider!.Value;
					string? externalUserId;
					try
					{
						var validator = oAuthProviders
							.GetValidator(oAuthProvider);

						if (validator == null)
							return BadRequest<TokenResponse>(ErrorCode.OAuthProviderDisabled);

						externalUserId = await validator
							.ValidateResponseCode(headers.OAuthCode!, cancellationToken);

						Logger.LogTrace("External {oAuthProvider} UID: {externalUserId}", oAuthProvider, externalUserId);
					}
					catch (Octokit.RateLimitExceededException ex)
					{
						return RateLimit<TokenResponse>(ex);
					}

					if (externalUserId == null)
						return Unauthorized<TokenResponse>();

					query = query.Where(
						x => x.OAuthConnections!.Any(
							y => y.Provider == oAuthProvider
							&& y.ExternalUserId == externalUserId));
				}
				else
				{
					var canonicalUserName = User.CanonicalizeName(headers.Username!);
					if (canonicalUserName == User.CanonicalizeName(User.TgsSystemUserName))
						return Unauthorized<TokenResponse>();

					if (systemIdentity == null)
						query = query.Where(x => x.CanonicalName == canonicalUserName);
					else
						query = query.Where(x => x.CanonicalName == canonicalUserName || x.SystemIdentifier == systemIdentity.Uid);
				}

				var user = await SelectUserInfoFromQuery(query, cancellationToken);

				// No user? You're not allowed
				if (user == null)
					return Unauthorized<TokenResponse>();

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
							return Unauthorized<TokenResponse>();
						if (user.PasswordHash != originalHash)
						{
							Logger.LogDebug("User ID {userId}'s password hash needs a refresh, updating database.", user.Id);
							var updatedUser = new User
							{
								Id = user.Id,
							};
							databaseContext.Users.Attach(updatedUser);
							updatedUser.PasswordHash = user.PasswordHash;
							await databaseContext.Save(cancellationToken);
						}
					}
					else
					{
						var usernameMismatch = systemIdentity!.Username != user.Name;
						if (isLikelyDbUser || usernameMismatch)
						{
							databaseContext.Users.Attach(user);
							if (isLikelyDbUser)
							{
								// cleanup from https://github.com/tgstation/tgstation-server/issues/1528
								Logger.LogDebug("System user ID {userId}'s PasswordHash is polluted, updating database.", user.Id);
								user.PasswordHash = null;
								user.LastPasswordUpdate = DateTimeOffset.UtcNow;
							}

							if (usernameMismatch)
							{
								// System identity username change update
								Logger.LogDebug("User ID {userId}'s system identity needs a refresh, updating database.", user.Id);
								user.Name = systemIdentity.Username;
								user.CanonicalName = User.CanonicalizeName(user.Name);
							}

							await databaseContext.Save(cancellationToken);
						}
					}

				// Now that the bookeeping is done, tell them to fuck off if necessary
				if (!user.Enabled!.Value)
				{
					Logger.LogTrace("Not logging in disabled user {userId}.", user.Id);
					return Forbid<TokenResponse>();
				}

				var token = tokenFactory.CreateToken(user, oAuthLogin);
				if (usingSystemIdentity)
					await CacheSystemIdentity(systemIdentity!, user, token);

				Logger.LogDebug("Successfully logged in user {userId}!", user.Id);

				return new AuthorityResponse<TokenResponse>(token);
			}
		}

		/// <summary>
		/// Add a given <paramref name="systemIdentity"/> to the <see cref="identityCache"/>.
		/// </summary>
		/// <param name="systemIdentity">The <see cref="ISystemIdentity"/> to cache.</param>
		/// <param name="user">The <see cref="User"/> the <paramref name="systemIdentity"/> was generated for.</param>
		/// <param name="token">The <see cref="TokenResponse"/> for the <paramref name="user"/>.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		private async ValueTask CacheSystemIdentity(ISystemIdentity systemIdentity, User user, TokenResponse token)
		{
			// expire the identity slightly after the auth token in case of lag
			var identExpiry = token.ParseJwt().ValidTo;
			identExpiry += tokenFactory.ValidationParameters.ClockSkew;
			identExpiry += TimeSpan.FromSeconds(15);
			await identityCache.CacheSystemIdentity(user, systemIdentity!, identExpiry);
		}
	}
}
