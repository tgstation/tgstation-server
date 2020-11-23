using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for <see cref="Administration"/> purposes
	/// </summary>
	[Route(Routes.Administration)]
	public sealed class AdministrationController : ApiController
	{
		const string OctokitException = "Bad GitHub API response, check configuration!";

		/// <summary>
		/// The <see cref="IGitHubClientFactory"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly IGitHubClientFactory gitHubClientFactory;

		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly IServerControl serverUpdater;

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="UpdatesConfiguration"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly UpdatesConfiguration updatesConfiguration;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// The <see cref="FileLoggingConfiguration"/> for the <see cref="AdministrationController"/>
		/// </summary>
		readonly FileLoggingConfiguration fileLoggingConfiguration;

		/// <summary>
		/// Construct an <see cref="AdministrationController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="gitHubClientFactory">The value of <see cref="gitHubClientFactory"/></param>
		/// <param name="serverUpdater">The value of <see cref="serverUpdater"/></param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		/// <param name="updatesConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="updatesConfiguration"/></param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="generalConfiguration"/></param>
		/// <param name="fileLoggingConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="fileLoggingConfiguration"/></param>
		public AdministrationController(
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			IGitHubClientFactory gitHubClientFactory,
			IServerControl serverUpdater,
			IAssemblyInformationProvider assemblyInformationProvider,
			IIOManager ioManager,
			IPlatformIdentifier platformIdentifier,
			ILogger<AdministrationController> logger,
			IOptions<UpdatesConfiguration> updatesConfigurationOptions,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<FileLoggingConfiguration> fileLoggingConfigurationOptions)
			: base(
				databaseContext,
				authenticationContextFactory,
				logger,
				true)
		{
			this.gitHubClientFactory = gitHubClientFactory ?? throw new ArgumentNullException(nameof(gitHubClientFactory));
			this.serverUpdater = serverUpdater ?? throw new ArgumentNullException(nameof(serverUpdater));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			updatesConfiguration = updatesConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(updatesConfigurationOptions));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			fileLoggingConfiguration = fileLoggingConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(fileLoggingConfigurationOptions));
		}

		/// <summary>
		/// Try to download and apply an update with a given <paramref name="newVersion"/>.
		/// </summary>
		/// <param name="newVersion">The version of the server to update to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		async Task<IActionResult> CheckReleasesAndApplyUpdate(Version newVersion, CancellationToken cancellationToken)
		{
			Logger.LogDebug("Looking for GitHub releases version {0}...", newVersion);
			IEnumerable<Release> releases;
			try
			{
				var gitHubClient = GetGitHubClient();
				releases = await gitHubClient
					.Repository
					.Release
					.GetAll(updatesConfiguration.GitHubRepositoryId)
					.WithToken(cancellationToken)
					.ConfigureAwait(false);
			}
			catch (RateLimitExceededException e)
			{
				return RateLimit(e);
			}
			catch (ApiException e)
			{
				Logger.LogWarning(e, OctokitException);
				return StatusCode(HttpStatusCode.FailedDependency);
			}

			releases = releases.Where(x => x.TagName.StartsWith(updatesConfiguration.GitTagPrefix, StringComparison.InvariantCulture));

			Logger.LogTrace("Release query complete!");

			foreach (var release in releases)
				if (Version.TryParse(
					release.TagName.Replace(
						updatesConfiguration.GitTagPrefix, String.Empty, StringComparison.Ordinal),
					out var version)
					&& version == newVersion)
				{
					var asset = release.Assets.Where(x => x.Name.Equals(updatesConfiguration.UpdatePackageAssetName, StringComparison.Ordinal)).FirstOrDefault();
					if (asset == default)
						continue;

					if (!serverUpdater.ApplyUpdate(version, new Uri(asset.BrowserDownloadUrl), ioManager))
						return Conflict(new ErrorMessage(ErrorCode.ServerUpdateInProgress));
					return Accepted(new Administration
					{
						WindowsHost = platformIdentifier.IsWindows,
						NewVersion = newVersion
					}); // gtfo of here before all the cancellation tokens fire
				}

			return Gone();
		}

		IGitHubClient GetGitHubClient() => String.IsNullOrEmpty(generalConfiguration.GitHubAccessToken) ? gitHubClientFactory.CreateClient() : gitHubClientFactory.CreateClient(generalConfiguration.GitHubAccessToken);

		/// <summary>
		/// Get <see cref="Administration"/> server information.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Retrieved <see cref="Administration"/> data successfully.</response>
		/// <response code="424">The GitHub API rate limit was hit. See response header Retry-After.</response>
		/// <response code="429">A GitHub API error occurred. See error message for details.</response>
		[HttpGet]
		[TgsAuthorize]
		[ProducesResponseType(typeof(Administration), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 424)]
		[ProducesResponseType(typeof(ErrorMessage), 429)]
		public async Task<IActionResult> Read(CancellationToken cancellationToken)
		{
			try
			{
				Version greatestVersion = null;
				Uri repoUrl = null;
				try
				{
					var gitHubClient = GetGitHubClient();
					var repositoryTask = gitHubClient
						.Repository
						.Get(updatesConfiguration.GitHubRepositoryId)
						.WithToken(cancellationToken);
					var releases = (await gitHubClient
						.Repository
						.Release
						.GetAll(updatesConfiguration.GitHubRepositoryId)
						.WithToken(cancellationToken)
						.ConfigureAwait(false))
						.Where(x => x.TagName.StartsWith(
							updatesConfiguration.GitTagPrefix,
							StringComparison.InvariantCulture));

					foreach (var I in releases)
						if (Version.TryParse(I.TagName.Replace(updatesConfiguration.GitTagPrefix, String.Empty, StringComparison.Ordinal), out var version)
							&& version.Major == assemblyInformationProvider.Version.Major
							&& (greatestVersion == null || version > greatestVersion))
							greatestVersion = version;
					repoUrl = new Uri((await repositoryTask.ConfigureAwait(false)).HtmlUrl);
				}
				catch (NotFoundException e)
				{
					Logger.LogWarning(e, "Not found exception while retrieving upstream repository info!");
				}

				return Json(new Administration
				{
					LatestVersion = greatestVersion,
					TrackedRepositoryUrl = repoUrl,
					WindowsHost = platformIdentifier.IsWindows
				});
			}
			catch (RateLimitExceededException e)
			{
				return RateLimit(e);
			}
			catch (ApiException e)
			{
				Logger.LogWarning(e, OctokitException);
				return StatusCode(HttpStatusCode.FailedDependency, new ErrorMessage(ErrorCode.GitHubApiError)
				{
					AdditionalData = e.Message
				});
			}
		}

		/// <summary>
		/// Attempt to perform a server upgrade.
		/// </summary>
		/// <param name="model">The model containing the <see cref="Administration.NewVersion"/> to update to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="202">Update has been started successfully.</response>
		/// <response code="410">The requested release version could not be found in the target GitHub repository.</response>
		/// <response code="422">Upgrade operations are unavailable due to the launch configuration of TGS.</response>
		/// <response code="424">A GitHub rate limit was encountered.</response>
		/// <response code="429">A GitHub API error occurred.</response>
		[HttpPost]
		[TgsAuthorize(AdministrationRights.ChangeVersion)]
		[ProducesResponseType(typeof(Administration), 202)]
		[ProducesResponseType(typeof(ErrorMessage), 410)]
		[ProducesResponseType(typeof(ErrorMessage), 422)]
		[ProducesResponseType(typeof(ErrorMessage), 424)]
		[ProducesResponseType(typeof(ErrorMessage), 429)]
		public async Task<IActionResult> Update([FromBody] Administration model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.NewVersion == null)
				return BadRequest(new ErrorMessage(ErrorCode.ModelValidationFailure)
				{
					AdditionalData = "newVersion is required!"
				});

			if (model.NewVersion.Major != assemblyInformationProvider.Version.Major)
				return BadRequest(new ErrorMessage(ErrorCode.CannotChangeServerSuite));

			if (!serverUpdater.WatchdogPresent)
				return UnprocessableEntity(new ErrorMessage(ErrorCode.MissingHostWatchdog));

			return await CheckReleasesAndApplyUpdate(model.NewVersion, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Attempts to restart the server.
		/// </summary>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request</returns>
		/// <response code="204">Restart begun successfully.</response>
		/// <response code="422">Restart operations are unavailable due to the launch configuration of TGS.</response>
		[HttpDelete]
		[TgsAuthorize(AdministrationRights.RestartHost)]
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(ErrorMessage), 422)]
		public async Task<IActionResult> Delete()
		{
			try
			{
				if (!serverUpdater.WatchdogPresent)
				{
					Logger.LogDebug("Restart request failed due to lack of host watchdog!");
					return UnprocessableEntity(new ErrorMessage(ErrorCode.MissingHostWatchdog));
				}

				await serverUpdater.Restart().ConfigureAwait(false);
				return NoContent();
			}
			catch (InvalidOperationException)
			{
				return StatusCode(HttpStatusCode.ServiceUnavailable);
			}
		}

		/// <summary>
		/// List <see cref="LogFile"/>s present.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Listed logs successfully.</response>
		/// <response code="409">An IO error occurred while listing.</response>
		[HttpGet(Routes.Logs)]
		[TgsAuthorize(AdministrationRights.DownloadLogs)]
		[ProducesResponseType(typeof(List<LogFile>), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 409)]
		public async Task<IActionResult> ListLogs(CancellationToken cancellationToken)
		{
			var path = fileLoggingConfiguration.GetFullLogDirectory(ioManager, assemblyInformationProvider, platformIdentifier);
			try
			{
				var files = await ioManager.GetFiles(path, cancellationToken).ConfigureAwait(false);
				var tasks = files.Select(
					async file => new LogFile
					{
						Name = ioManager.GetFileName(file),
						LastModified = await ioManager.GetLastModified(
							ioManager.ConcatPath(path, file),
							cancellationToken)
						.ConfigureAwait(false)
					})
					.ToList();

				await Task.WhenAll(tasks).ConfigureAwait(false);

				var result = tasks
					.Select(x => x.Result)
					.OrderByDescending(x => x.Name)
					.ToList();

				return Ok(result);
			}
			catch (IOException ex)
			{
				return Conflict(new ErrorMessage(ErrorCode.IOError)
				{
					AdditionalData = ex.ToString()
				});
			}
		}

		/// <summary>
		/// Download a <see cref="LogFile"/>.
		/// </summary>
		/// <param name="path">The path to download.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Downloaded <see cref="LogFile"/> successfully.</response>
		/// <response code="409">An IO error occurred while downloading.</response>
		[HttpGet(Routes.Logs + "/{*path}")]
		[TgsAuthorize(AdministrationRights.DownloadLogs)]
		[ProducesResponseType(typeof(LogFile), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 409)]
		public async Task<IActionResult> GetLog(string path, CancellationToken cancellationToken)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			path = HttpUtility.UrlDecode(path);

			// guard against directory navigation
			var sanitizedPath = ioManager.GetFileName(path);
			if (path != sanitizedPath)
				return Forbid();

			var fullPath = ioManager.ConcatPath(
				fileLoggingConfiguration.GetFullLogDirectory(ioManager, assemblyInformationProvider, platformIdentifier),
				path);
			try
			{
				var readTask = ioManager.ReadAllBytes(fullPath, cancellationToken);

				return Ok(new LogFile
				{
					Name = path,
					LastModified = await ioManager.GetLastModified(fullPath, cancellationToken).ConfigureAwait(false),
					Content = await readTask.ConfigureAwait(false)
				});
			}
			catch (IOException ex)
			{
				return Conflict(new ErrorMessage(ErrorCode.IOError)
				{
					AdditionalData = ex.ToString()
				});
			}
		}
	}
}
