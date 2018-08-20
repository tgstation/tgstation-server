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

		/// <summary>
		/// The <see cref="IGitHubClient"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly IGitHubClient gitHubClient;

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
		/// Construct an <see cref="AdministrationController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="gitHubClient">The value of <see cref="gitHubClient"/></param>
		/// <param name="serverUpdater">The value of <see cref="serverUpdater"/></param>
		/// <param name="application">The value of <see cref="application"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		/// <param name="updatesConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="updatesConfiguration"/></param>
		public AdministrationController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IGitHubClient gitHubClient, IServerControl serverUpdater, IApplication application, IIOManager ioManager, ILogger<AdministrationController> logger, IOptions<UpdatesConfiguration> updatesConfigurationOptions) : base(databaseContext, authenticationContextFactory, logger, false)
		{
			this.gitHubClient = gitHubClient ?? throw new ArgumentNullException(nameof(gitHubClient));
			this.serverUpdater = serverUpdater ?? throw new ArgumentNullException(nameof(serverUpdater));
			this.application = application ?? throw new ArgumentNullException(nameof(application));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			updatesConfiguration = updatesConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(updatesConfigurationOptions));
		}
		
		StatusCodeResult RateLimit(RateLimitExceededException exception)
		{
			Logger.LogWarning("Exceeded GitHub rate limit!");
			var secondsString = Math.Ceiling((exception.Reset - DateTimeOffset.Now).TotalSeconds).ToString(CultureInfo.InvariantCulture);
			Response.Headers.Add("Retry-After", new StringValues(secondsString));
			return StatusCode(429);
		}

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

			IEnumerable<Release> releases;
			try
			{
				releases = (await gitHubClient.Repository.Release.GetAll(updatesConfiguration.GitHubRepositoryId).ConfigureAwait(false)).Where(x => x.TagName.StartsWith(updatesConfiguration.GitTagPrefix, StringComparison.InvariantCulture));
			}
			catch (RateLimitExceededException e)
			{
				return RateLimit(e);
			}

			foreach (var release in releases)
				if (Version.TryParse(release.TagName.Replace(updatesConfiguration.GitTagPrefix, String.Empty, StringComparison.Ordinal), out var version) && version == model.NewVersion)
				{
					var asset = release.Assets.Where(x => x.Name == updatesConfiguration.UpdatePackageAssetName).FirstOrDefault();
					if (asset == default)
						continue;

					var assetBytes = await ioManager.DownloadFile(new Uri(asset.BrowserDownloadUrl), cancellationToken).ConfigureAwait(false);
					try
					{
						if (!await serverUpdater.ApplyUpdate(assetBytes, ioManager, cancellationToken).ConfigureAwait(false))
							return UnprocessableEntity(new ErrorMessage
							{
								Message = RestartNotSupportedException
							});	//unprocessable entity
					}
					catch (InvalidOperationException)
					{
						return StatusCode((int)HttpStatusCode.ServiceUnavailable);  //we were beat to the punch, really shouldn't happen but heat death of the universe and what not
					}
					return Accepted();	//gtfo of here before all the cancellation tokens fire
				}

			return StatusCode((int)HttpStatusCode.Gone);
		}

		/// <inheritdoc />
		[HttpDelete]
		[TgsAuthorize(AdministrationRights.RestartHost)]
		public Task<IActionResult> Delete() {
			try
			{
				return Task.FromResult(serverUpdater.Restart() ? (IActionResult)Ok() : UnprocessableEntity(new ErrorMessage
				{
					Message = RestartNotSupportedException
				}));
			}
			catch (InvalidOperationException)
			{
				return Task.FromResult<IActionResult>(StatusCode((int)HttpStatusCode.ServiceUnavailable));
			}
		}
	}
}
