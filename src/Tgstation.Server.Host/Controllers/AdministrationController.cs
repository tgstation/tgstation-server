using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ModelController{TModel}"/> for <see cref="Administration"/>
	/// </summary>
	[Route(Routes.Administration)]
	public sealed class AdministrationController : ModelController<Administration>
	{
		const string RestartNotSupportedException = "This deployment of tgstation-server is lacking the Tgstation.Server.Host.Watchdog component. Restarts and version changes cannot be completed!";

		const string OctokitException = "Bad GitHub API response, check configuration! Exception: {0}";

		/// <summary>
		/// The <see cref="IGitHubClientFactory"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly IGitHubClientFactory gitHubClientFactory;

		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly IServerControl serverUpdater;

		/// <summary>
		/// The <see cref="IApplication"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly IApplication application;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="UpdatesConfiguration"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly UpdatesConfiguration updatesConfiguration;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// Construct an <see cref="AdministrationController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="gitHubClientFactory">The value of <see cref="gitHubClientFactory"/></param>
		/// <param name="serverUpdater">The value of <see cref="serverUpdater"/></param>
		/// <param name="application">The value of <see cref="application"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		/// <param name="updatesConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="updatesConfiguration"/></param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="generalConfiguration"/></param>
		public AdministrationController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IGitHubClientFactory gitHubClientFactory, IServerControl serverUpdater, IApplication application, IIOManager ioManager, ILogger<AdministrationController> logger, IOptions<UpdatesConfiguration> updatesConfigurationOptions, IOptions<GeneralConfiguration> generalConfigurationOptions) : base(databaseContext, authenticationContextFactory, logger, false)
		{
			this.gitHubClientFactory = gitHubClientFactory ?? throw new ArgumentNullException(nameof(gitHubClientFactory));
			this.serverUpdater = serverUpdater ?? throw new ArgumentNullException(nameof(serverUpdater));
			this.application = application ?? throw new ArgumentNullException(nameof(application));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			updatesConfiguration = updatesConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(updatesConfigurationOptions));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		StatusCodeResult RateLimit(RateLimitExceededException exception)
		{
			Logger.LogWarning("Exceeded GitHub rate limit! Exception {0}", exception);
			var secondsString = Math.Ceiling((exception.Reset - DateTimeOffset.Now).TotalSeconds).ToString(CultureInfo.InvariantCulture);
			Response.Headers.Add("Retry-After", new StringValues(secondsString));
			return StatusCode(429);
		}

		IGitHubClient GetGitHubClient() => String.IsNullOrEmpty(generalConfiguration.GitHubAccessToken) ? gitHubClientFactory.CreateClient() : gitHubClientFactory.CreateClient(generalConfiguration.GitHubAccessToken);

		/// <inheritdoc />
		[TgsAuthorize]
		public override async Task<IActionResult> Read(CancellationToken cancellationToken)
		{
			try
			{
				Version greatestVersion = null;
				Uri repoUrl = null;
				try
				{
					var gitHubClient = GetGitHubClient();
					var repositoryTask = gitHubClient.Repository.Get(updatesConfiguration.GitHubRepositoryId);
					var releases = (await gitHubClient.Repository.Release.GetAll(updatesConfiguration.GitHubRepositoryId).ConfigureAwait(false)).Where(x => x.TagName.StartsWith(updatesConfiguration.GitTagPrefix, StringComparison.InvariantCulture));

					foreach (var I in releases)
						if (Version.TryParse(I.TagName.Replace(updatesConfiguration.GitTagPrefix, String.Empty, StringComparison.Ordinal), out var version)
							&& version.Major == application.Version.Major
							&& (greatestVersion == null || version > greatestVersion))
							greatestVersion = version;
					repoUrl = new Uri((await repositoryTask.ConfigureAwait(false)).HtmlUrl);
				}
				catch (NotFoundException e)
				{
					Logger.LogWarning("Not found exception while retrieving upstream repository info: {0}", e);
				}
				return Json(new Administration
				{
					LatestVersion = greatestVersion,
					TrackedRepositoryUrl = repoUrl,
					WindowsHost = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
				});
			}
			catch (RateLimitExceededException e)
			{
				return RateLimit(e);
			}
			catch (ApiException e)
			{
				Logger.LogWarning(OctokitException, e);
				return StatusCode((int)HttpStatusCode.FailedDependency);
			}
		}

		/// <inheritdoc />
		[TgsAuthorize(AdministrationRights.ChangeVersion)]
		public override async Task<IActionResult> Update([FromBody] Administration model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.NewVersion == null)
				return BadRequest(new ErrorMessage { Message = "Missing new version!" });

			if (model.NewVersion.Major != application.Version.Major)
				return BadRequest(new ErrorMessage { Message = "Cannot update to a different suite version!" });

			Logger.LogInformation("Updating to version {0}...", model.NewVersion);
			IEnumerable<Release> releases;
			try
			{
				var gitHubClient = GetGitHubClient();
				releases = (await gitHubClient.Repository.Release.GetAll(updatesConfiguration.GitHubRepositoryId).ConfigureAwait(false)).Where(x => x.TagName.StartsWith(updatesConfiguration.GitTagPrefix, StringComparison.InvariantCulture));
			}
			catch (RateLimitExceededException e)
			{
				return RateLimit(e);
			}
			catch (ApiException e)
			{
				Logger.LogWarning(OctokitException, e);
				return StatusCode((int)HttpStatusCode.FailedDependency);
			}

			Logger.LogTrace("Release query complete!");
			foreach (var release in releases)
				if (Version.TryParse(release.TagName.Replace(updatesConfiguration.GitTagPrefix, String.Empty, StringComparison.Ordinal), out var version) && version == model.NewVersion)
				{
					var asset = release.Assets.Where(x => x.Name == updatesConfiguration.UpdatePackageAssetName).FirstOrDefault();
					if (asset == default)
						continue;

					Logger.LogDebug("Downloading update package...");
					var assetBytes = await ioManager.DownloadFile(new Uri(asset.BrowserDownloadUrl), cancellationToken).ConfigureAwait(false);
					try
					{
						Logger.LogDebug("Extracting server update...");
						if (!await serverUpdater.ApplyUpdate(version, assetBytes, ioManager, cancellationToken).ConfigureAwait(false))
							return UnprocessableEntity(new ErrorMessage
							{
								Message = RestartNotSupportedException
							}); //unprocessable entity
					}
					catch (InvalidOperationException)
					{
						Logger.LogDebug("Unable to find github release version!");
						return StatusCode((int)HttpStatusCode.ServiceUnavailable);  //we were beat to the punch, really shouldn't happen but heat death of the universe and what not
					}
					Logger.LogInformation("Update staged! Restarting host...");
					return Accepted();  //gtfo of here before all the cancellation tokens fire
				}

			return StatusCode((int)HttpStatusCode.Gone);
		}

		/// <inheritdoc />
		[HttpDelete]
		[TgsAuthorize(AdministrationRights.RestartHost)]
		public async Task<IActionResult> Delete()
		{
			try
			{
				var result = await serverUpdater.Restart().ConfigureAwait(false);
				if (result)
					Logger.LogInformation("Restarting host by request...");
				else
					Logger.LogDebug("Restart request failed due to lack of host watchdog!");
				return result ? (IActionResult)Ok() : UnprocessableEntity(new ErrorMessage
				{
					Message = RestartNotSupportedException
				});
			}
			catch (InvalidOperationException)
			{
				return StatusCode((int)HttpStatusCode.ServiceUnavailable);
			}
		}
	}
}
