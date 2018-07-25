using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
	[Route("/" + nameof(Administration))]
	sealed class AdministrationController : ModelController<Administration>
	{
		/// <summary>
		/// HTTP 429 status code
		/// </summary>
		const int RateLimitHttpStatusCode = 429;

		/// <summary>
		/// The <see cref="IGitHubClient"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly IGitHubClient gitHubClient;

		/// <summary>
		/// The <see cref="IServerUpdater"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly IServerUpdater serverUpdater;

		/// <summary>
		/// The <see cref="IApplication"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly IApplication application;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly ILogger<AdministrationController> logger;

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
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="updatesConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="updatesConfiguration"/></param>
		public AdministrationController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IGitHubClient gitHubClient, IServerUpdater serverUpdater, IApplication application, IIOManager ioManager, ILogger<AdministrationController> logger, IOptions<UpdatesConfiguration> updatesConfigurationOptions) : base(databaseContext, authenticationContextFactory)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.gitHubClient = gitHubClient ?? throw new ArgumentNullException(nameof(gitHubClient));
			this.serverUpdater = serverUpdater ?? throw new ArgumentNullException(nameof(serverUpdater));
			this.application = application ?? throw new ArgumentNullException(nameof(application));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			updatesConfiguration = updatesConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(updatesConfigurationOptions));
		}
		
		StatusCodeResult RateLimit(RateLimitExceededException exception)
		{
			logger.LogWarning("Exceeded GitHub rate limit!");
			var secondsString = Math.Ceiling((exception.Reset - DateTimeOffset.Now).TotalSeconds).ToString(CultureInfo.InvariantCulture);
			Response.Headers.Add("Retry-After", new Microsoft.Extensions.Primitives.StringValues { });
			return StatusCode(RateLimitHttpStatusCode);
		}

		/// <inheritdoc />
		[TgsAuthorize]
		public override async Task<IActionResult> Read(CancellationToken cancellationToken)
		{
			var model = new Administration
			{
				CurrentVersion = application.Version
			};
			try
			{
				var repositoryTask = gitHubClient.Repository.Get(updatesConfiguration.GitHubRepositoryId);
				var releases = (await gitHubClient.Repository.Release.GetAll(updatesConfiguration.GitHubRepositoryId).ConfigureAwait(false)).Where(x => x.TagName.StartsWith(updatesConfiguration.GitTagPrefix, StringComparison.InvariantCulture));

				Version greatestVersion = null;
				foreach (var I in releases)
					if (Version.TryParse(I.TagName.Replace(updatesConfiguration.GitTagPrefix, String.Empty), out var version)
						&& version.Major == application.Version.Major
						&& (greatestVersion == null || version > greatestVersion))
						greatestVersion = version;

				model.LatestVersion = greatestVersion;
				model.TrackedRepositoryUrl = new Uri((await repositoryTask.ConfigureAwait(false)).HtmlUrl);
			}
			catch (RateLimitExceededException e)
			{
				return RateLimit(e);
			}
			return Json(model);
		}

		/// <inheritdoc />
		[TgsAuthorize(AdministrationRights.ChangeVersion)]
		public override async Task<IActionResult> Update([FromBody] Administration model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.CurrentVersion == null)
				return BadRequest(new { message = "Missing new version!" });

			if (model.CurrentVersion.Major != application.Version.Major)
				return BadRequest(new { message = "Cannot update to a different suite version!" });

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
				if (Version.TryParse(release.TagName.Replace(updatesConfiguration.GitTagPrefix, String.Empty), out var version) && version == model.CurrentVersion)
				{
					var asset = release.Assets.Where(x => x.Name == updatesConfiguration.UpdatePackageAssetName).FirstOrDefault();
					if (asset == default)
						continue;

					var assetBytes = await ioManager.DownloadFile(new Uri(asset.Url), cancellationToken).ConfigureAwait(false);
					await serverUpdater.ApplyUpdate(assetBytes, ioManager, cancellationToken).ConfigureAwait(false);
					return Ok();	//gtfo of here before all the cancellation tokens fire
				}

			return StatusCode((int)HttpStatusCode.Gone);
		}
	}
}
