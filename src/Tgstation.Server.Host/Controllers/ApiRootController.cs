using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

using Octokit;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.GraphQL.Mutations;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Security.OAuth;
using Tgstation.Server.Host.Swarm;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Root <see cref="ApiController"/> for the <see cref="Application"/>.
	/// </summary>
	[Route(Routes.ApiRoot)]
	public sealed class ApiRootController : ApiController
	{
		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="ApiRootController"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="IOAuthProviders"/> for the <see cref="ApiRootController"/>.
		/// </summary>
		readonly IOAuthProviders oAuthProviders;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="ApiRootController"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="ISwarmService"/> for the <see cref="ApiRootController"/>.
		/// </summary>
		readonly ISwarmService swarmService;

		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="ApiRootController"/>.
		/// </summary>
		readonly IServerControl serverControl;

		/// <summary>
		/// The <see cref="IRestAuthorityInvoker{TAuthority}"/> for the <see cref="ILoginAuthority"/>.
		/// </summary>
		readonly IRestAuthorityInvoker<ILoginAuthority> loginAuthority;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="ApiRootController"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiRootController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="oAuthProviders">The value of <see cref="oAuthProviders"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="swarmService">The value of <see cref="swarmService"/>.</param>
		/// <param name="serverControl">The value of <see cref="serverControl"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/>.</param>
		/// <param name="apiHeadersProvider">The <see cref="IApiHeadersProvider"/> for the <see cref="ApiController"/>.</param>
		/// <param name="loginAuthority">The value of <see cref="loginAuthority"/>.</param>
		public ApiRootController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			IAssemblyInformationProvider assemblyInformationProvider,
			IOAuthProviders oAuthProviders,
			IPlatformIdentifier platformIdentifier,
			ISwarmService swarmService,
			IServerControl serverControl,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			ILogger<ApiRootController> logger,
			IApiHeadersProvider apiHeadersProvider,
			IRestAuthorityInvoker<ILoginAuthority> loginAuthority)
			: base(
				  databaseContext,
				  authenticationContext,
				  apiHeadersProvider,
				  logger,
				  false)
		{
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.oAuthProviders = oAuthProviders ?? throw new ArgumentNullException(nameof(oAuthProviders));
			this.swarmService = swarmService ?? throw new ArgumentNullException(nameof(swarmService));
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			this.loginAuthority = loginAuthority ?? throw new ArgumentNullException(nameof(loginAuthority));
		}

		/// <summary>
		/// Main page of the <see cref="Application"/>.
		/// </summary>
		/// <returns>
		/// The <see cref="JsonResult"/> containing <see cref="ServerInformationResponse"/> of the <see cref="Application"/> if a properly authenticated API request, the web control panel if on a browser and enabled, <see cref="UnauthorizedResult"/> otherwise.
		/// </returns>
		/// <response code="200"><see cref="ServerInformationResponse"/> retrieved successfully.</response>
		[HttpGet]
		[AllowAnonymous]
		[ProducesResponseType(typeof(ServerInformationResponse), 200)]
		public IActionResult ServerInfo()
		{
			// if they tried to authenticate in any form and failed, let them know immediately
			bool failIfUnauthed;
			if (ApiHeaders == null)
			{
				try
				{
					// we only allow authorization header issues
					ApiHeadersProvider.CreateAuthlessHeaders();
				}
				catch (HeadersException ex)
				{
					return HeadersIssue(ex);
				}

				failIfUnauthed = Request.Headers.Authorization.Count > 0;
			}
			else
				failIfUnauthed = ApiHeaders.Token != null;

			if (failIfUnauthed && !AuthenticationContext.Valid)
				return Unauthorized();

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
				SwarmServers = swarmService
					.GetSwarmServers()
					?.Select(swarmServerInfo => new SwarmServerResponse(swarmServerInfo))
					.ToList(),
				OAuthProviderInfos = oAuthProviders.ProviderInfos(),
				UpdateInProgress = serverControl.UpdateInProgress,
			});
		}

		/// <summary>
		/// Attempt to authenticate a <see cref="User"/> using <see cref="ApiController.ApiHeaders"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">User logged in and <see cref="TokenResponse"/> generated successfully.</response>
		/// <response code="401">User authentication failed.</response>
		/// <response code="403">User authenticated but is disabled by an administrator.</response>
		/// <response code="429">OAuth authentication failed due to rate limiting.</response>
		[HttpPost]
		[ProducesResponseType(typeof(TokenResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 429)]
		public ValueTask<IActionResult> CreateToken(CancellationToken cancellationToken)
		{
			if (ApiHeaders == null)
			{
				Response.Headers.Add(HeaderNames.WWWAuthenticate, new StringValues($"basic realm=\"Create TGS {ApiHeaders.BearerAuthenticationScheme} token\""));
				return ValueTask.FromResult(HeadersIssue(ApiHeadersProvider.HeadersException!));
			}

			return loginAuthority.InvokeTransformable<LoginPayload, TokenResponse>(this, authority => authority.AttemptLogin(cancellationToken));
		}
	}
}
