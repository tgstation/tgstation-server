using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Octokit;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Security.OAuth;
using Tgstation.Server.Host.Swarm;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Root <see cref="ApiController"/> for the <see cref="Application"/>.
	/// </summary>
	[Route(Routes.Root)]
	public sealed class HomeController : ApiController
	{
		/// <summary>
		/// The <see cref="ITokenFactory"/> for the <see cref="HomeController"/>.
		/// </summary>
		readonly ITokenFactory tokenFactory;

		/// <summary>
		/// The <see cref="ISystemIdentityFactory"/> for the <see cref="HomeController"/>.
		/// </summary>
		readonly ISystemIdentityFactory systemIdentityFactory;

		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="HomeController"/>.
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="HomeController"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="IIdentityCache"/> for the <see cref="HomeController"/>.
		/// </summary>
		readonly IIdentityCache identityCache;

		/// <summary>
		/// The <see cref="IOAuthProviders"/> for the <see cref="HomeController"/>.
		/// </summary>
		readonly IOAuthProviders oAuthProviders;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="HomeController"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="ISwarmService"/> for the <see cref="HomeController"/>.
		/// </summary>
		readonly ISwarmService swarmService;

		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="HomeController"/>.
		/// </summary>
		readonly IServerControl serverControl;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="HomeController"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// The <see cref="ControlPanelConfiguration"/> for the <see cref="HomeController"/>.
		/// </summary>
		readonly ControlPanelConfiguration controlPanelConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="HomeController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/>.</param>
		/// <param name="tokenFactory">The value of <see cref="tokenFactory"/>.</param>
		/// <param name="systemIdentityFactory">The value of <see cref="systemIdentityFactory"/>.</param>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="identityCache">The value of <see cref="identityCache"/>.</param>
		/// <param name="oAuthProviders">The value of <see cref="oAuthProviders"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="swarmService">The value of <see cref="swarmService"/>.</param>
		/// <param name="serverControl">The value of <see cref="serverControl"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="controlPanelConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="controlPanelConfiguration"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/>.</param>
		public HomeController(
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			ITokenFactory tokenFactory,
			ISystemIdentityFactory systemIdentityFactory,
			ICryptographySuite cryptographySuite,
			IAssemblyInformationProvider assemblyInformationProvider,
			IIdentityCache identityCache,
			IOAuthProviders oAuthProviders,
			IPlatformIdentifier platformIdentifier,
			ISwarmService swarmService,
			IServerControl serverControl,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<ControlPanelConfiguration> controlPanelConfigurationOptions,
			ILogger<HomeController> logger)
			: base(
				  databaseContext,
				  authenticationContextFactory,
				  logger,
				  false)
		{
			this.tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
			this.systemIdentityFactory = systemIdentityFactory ?? throw new ArgumentNullException(nameof(systemIdentityFactory));
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.identityCache = identityCache ?? throw new ArgumentNullException(nameof(identityCache));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.oAuthProviders = oAuthProviders ?? throw new ArgumentNullException(nameof(oAuthProviders));
			this.swarmService = swarmService ?? throw new ArgumentNullException(nameof(swarmService));
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			controlPanelConfiguration = controlPanelConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(controlPanelConfigurationOptions));
		}

		/// <summary>
		/// Main page of the <see cref="Application"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>
		/// A <see cref="Task{TResult}"/> resuting in the <see cref="JsonResult"/> containing <see cref="ServerInformationResponse"/> of the <see cref="Application"/> if a properly authenticated API request, the web control panel if on a browser and enabled, <see cref="UnauthorizedResult"/> otherwise.
		/// </returns>
		/// <response code="200"><see cref="ServerInformationResponse"/> retrieved successfully.</response>
		[HttpGet]
		[AllowAnonymous]
		[ProducesResponseType(typeof(ServerInformationResponse), 200)]
#pragma warning disable CA1506
		public async Task<IActionResult> Home(CancellationToken cancellationToken)
		{
			if (controlPanelConfiguration.Enable)
				Response.Headers.Add(
					HeaderNames.Vary,
					new StringValues(ApiHeaders.ApiVersionHeader));

			if (ApiHeaders == null)
			{
				if (controlPanelConfiguration.Enable && !Request.Headers.TryGetValue(ApiHeaders.ApiVersionHeader, out _))
				{
					Logger.LogDebug("No API headers on request, redirecting to control panel...");

					var controlPanelRoute = controlPanelConfiguration.PublicPath;
					if (String.IsNullOrWhiteSpace(controlPanelRoute))
						controlPanelRoute = ControlPanelController.ControlPanelRoute;

					return Redirect(controlPanelRoute);
				}

				try
				{
					// we only allow authorization header issues
					var headers = new ApiHeaders(Request.GetTypedHeaders(), true);
					if (!headers.Compatible())
						return StatusCode(
							HttpStatusCode.UpgradeRequired,
							new ErrorMessageResponse(ErrorCode.ApiMismatch));
				}
				catch (HeadersException)
				{
					return HeadersIssue(true);
				}
			}

			return Json(new ServerInformationResponse
			{
				Version = assemblyInformationProvider.Version,
				ApiVersion = ApiHeaders.Version,
				DMApiVersion = DMApiConstants.InteropVersion,
				MinimumPasswordLength = generalConfiguration.MinimumPasswordLength,
				InstanceLimit = generalConfiguration.InstanceLimit,
				UserLimit = generalConfiguration.UserLimit,
				UserGroupLimit = generalConfiguration.UserGroupLimit,
				ValidInstancePaths = generalConfiguration.ValidInstancePaths,
				WindowsHost = platformIdentifier.IsWindows,
				SwarmServers = swarmService.GetSwarmServers(),
				OAuthProviderInfos = await oAuthProviders.ProviderInfos(cancellationToken),
				UpdateInProgress = serverControl.UpdateInProgress,
			});
		}
#pragma warning restore CA1506

		/// <summary>
		/// Attempt to authenticate a <see cref="User"/> using <see cref="ApiController.ApiHeaders"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">User logged in and <see cref="TokenResponse"/> generated successfully.</response>
		/// <response code="401">User authentication failed.</response>
		/// <response code="403">User authenticated but is disabled by an administrator.</response>
		/// <response code="429">OAuth authentication failed due to rate limiting.</response>
		[HttpPost]
		[ProducesResponseType(typeof(TokenResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 429)]
#pragma warning disable CA1506 // TODO: Decomplexify
		public async Task<IActionResult> CreateToken(CancellationToken cancellationToken)
		{
			if (ApiHeaders == null)
			{
				Response.Headers.Add(HeaderNames.WWWAuthenticate, new StringValues("basic realm=\"Create TGS bearer token\""));
				return HeadersIssue(false);
			}

			if (ApiHeaders.IsTokenAuthentication)
				return BadRequest(new ErrorMessageResponse(ErrorCode.TokenWithToken));

			var oAuthLogin = ApiHeaders.OAuthProvider.HasValue;

			ISystemIdentity systemIdentity = null;
			if (!oAuthLogin)
				try
				{
					// trust the system over the database because a user's name can change while still having the same SID
					systemIdentity = await systemIdentityFactory.CreateSystemIdentity(ApiHeaders.Username, ApiHeaders.Password, cancellationToken);
				}
				catch (NotImplementedException ex)
				{
					Logger.LogTrace(ex, "System identities not implemented!");
				}

			using (systemIdentity)
			{
				// Get the user from the database
				IQueryable<Models.User> query = DatabaseContext.Users.AsQueryable();
				if (oAuthLogin)
				{
					var oAuthProvider = ApiHeaders.OAuthProvider.Value;
					string externalUserId;
					try
					{
						var validator = oAuthProviders
							.GetValidator(oAuthProvider);

						if (validator == null)
							return BadRequest(new ErrorMessageResponse(ErrorCode.OAuthProviderDisabled));

						externalUserId = await validator
							.ValidateResponseCode(ApiHeaders.Token, cancellationToken);

						Logger.LogTrace("External {oAuthProvider} UID: {externalUserId}", oAuthProvider, externalUserId);
					}
					catch (RateLimitExceededException ex)
					{
						return RateLimit(ex);
					}

					if (externalUserId == null)
						return Unauthorized();

					query = query.Where(
						x => x.OAuthConnections.Any(
							y => y.Provider == oAuthProvider
							&& y.ExternalUserId == externalUserId));
				}
				else
				{
					var canonicalUserName = Models.User.CanonicalizeName(ApiHeaders.Username);
					if (canonicalUserName == Models.User.CanonicalizeName(Models.User.TgsSystemUserName))
						return Unauthorized();

					if (systemIdentity == null)
						query = query.Where(x => x.CanonicalName == canonicalUserName);
					else
						query = query.Where(x => x.CanonicalName == canonicalUserName || x.SystemIdentifier == systemIdentity.Uid);
				}

				var users = await query
					.Select(x => new Models.User
					{
						Id = x.Id,
						PasswordHash = x.PasswordHash,
						Enabled = x.Enabled,
						Name = x.Name,
					})
					.ToListAsync(cancellationToken)
					;

				// Pick the DB user first
				var user = users
					.OrderByDescending(dbUser => dbUser.SystemIdentifier == null)
					.FirstOrDefault();

				// No user? You're not allowed
				if (user == null)
					return Unauthorized();

				// A system user may have had their name AND password changed to one in our DB...
				// Or a DB user was created that had the same user/pass as a system user
				// Dumb admins...
				// FALLBACK TO THE DB USER HERE, DO NOT REVEAL A SYSTEM LOGIN!!!
				// This of course, allows system users to discover TGS users in this (HIGHLY IMPROBABLE) case but that is not our fault
				var originalHash = user.PasswordHash;
				var isDbUser = originalHash != null;
				bool usingSystemIdentity = systemIdentity != null && !isDbUser;
				if (!oAuthLogin)
					if (!usingSystemIdentity)
					{
						// DB User password check and update
						if (!cryptographySuite.CheckUserPassword(user, ApiHeaders.Password))
							return Unauthorized();
						if (user.PasswordHash != originalHash)
						{
							Logger.LogDebug("User ID {userId}'s password hash needs a refresh, updating database.", user.Id);
							var updatedUser = new Models.User
							{
								Id = user.Id,
							};
							DatabaseContext.Users.Attach(updatedUser);
							updatedUser.PasswordHash = user.PasswordHash;
							await DatabaseContext.Save(cancellationToken);
						}
					}
					else if (systemIdentity.Username != user.Name)
					{
						// System identity username change update
						Logger.LogDebug("User ID {userId}'s system identity needs a refresh, updating database.", user.Id);
						DatabaseContext.Users.Attach(user);
						user.Name = systemIdentity.Username;
						user.CanonicalName = Models.User.CanonicalizeName(user.Name);
						await DatabaseContext.Save(cancellationToken);
					}

				// Now that the bookeeping is done, tell them to fuck off if necessary
				if (!user.Enabled.Value)
				{
					Logger.LogTrace("Not logging in disabled user {userId}.", user.Id);
					return Forbid();
				}

				var token = await tokenFactory.CreateToken(user, oAuthLogin, cancellationToken);
				if (usingSystemIdentity)
				{
					// expire the identity slightly after the auth token in case of lag
					var identExpiry = token.ExpiresAt;
					identExpiry += tokenFactory.ValidationParameters.ClockSkew;
					identExpiry += TimeSpan.FromSeconds(15);
					identityCache.CacheSystemIdentity(user, systemIdentity, identExpiry);
				}

				Logger.LogDebug("Successfully logged in user {userId}!", user.Id);

				return Json(token);
			}
		}
#pragma warning restore CA1506
	}
}
