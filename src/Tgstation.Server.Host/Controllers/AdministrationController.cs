using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Octokit;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers.Results;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Transfer;
using Tgstation.Server.Host.Utils;
using Tgstation.Server.Host.Utils.GitHub;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for TGS administration purposes.
	/// </summary>
	[Route(Routes.Administration)]
	public sealed class AdministrationController : ApiController
	{
		/// <summary>
		/// Default <see cref="Exception.Message"/> for <see cref="ApiException"/>s.
		/// </summary>
		const string OctokitException = "Bad GitHub API response, check configuration!";

		/// <summary>
		/// The <see cref="IMemoryCache"/> key for <see cref="Read(bool?, CancellationToken)"/>.
		/// </summary>
		static readonly object ReadCacheKey = new();

		/// <summary>
		/// The <see cref="IGitHubServiceFactory"/> for the <see cref="AdministrationController"/>.
		/// </summary>
		readonly IGitHubServiceFactory gitHubServiceFactory;

		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="AdministrationController"/>.
		/// </summary>
		readonly IServerControl serverControl;

		/// <summary>
		/// The <see cref="IServerUpdateInitiator"/> for the <see cref="AdministrationController"/>.
		/// </summary>
		readonly IServerUpdateInitiator serverUpdateInitiator;

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="AdministrationController"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="AdministrationController"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="AdministrationController"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="IFileTransferTicketProvider"/> for the <see cref="AdministrationController"/>.
		/// </summary>
		readonly IFileTransferTicketProvider fileTransferService;

		/// <summary>
		/// The <see cref="IMemoryCache"/> for the <see cref="AdministrationController"/>.
		/// </summary>
		readonly IMemoryCache cacheService;

		/// <summary>
		/// The <see cref="FileLoggingConfiguration"/> for the <see cref="AdministrationController"/>.
		/// </summary>
		readonly FileLoggingConfiguration fileLoggingConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="AdministrationController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="gitHubServiceFactory">The value of <see cref="gitHubServiceFactory"/>.</param>
		/// <param name="serverControl">The value of <see cref="serverControl"/>.</param>
		/// <param name="serverUpdateInitiator">The value of <see cref="serverUpdateInitiator"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="fileTransferService">The value of <see cref="fileTransferService"/>.</param>
		/// <param name="cacheService">The value of <see cref="cacheService"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/>.</param>
		/// <param name="fileLoggingConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="fileLoggingConfiguration"/>.</param>
		/// <param name="apiHeadersProvider">The <see cref="IApiHeadersProvider"/> for the <see cref="ApiController"/>.</param>
		public AdministrationController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			IGitHubServiceFactory gitHubServiceFactory,
			IServerControl serverControl,
			IServerUpdateInitiator serverUpdateInitiator,
			IAssemblyInformationProvider assemblyInformationProvider,
			IIOManager ioManager,
			IPlatformIdentifier platformIdentifier,
			IFileTransferTicketProvider fileTransferService,
			IMemoryCache cacheService,
			ILogger<AdministrationController> logger,
			IOptions<FileLoggingConfiguration> fileLoggingConfigurationOptions,
			IApiHeadersProvider apiHeadersProvider)
			: base(
				databaseContext,
				authenticationContext,
				apiHeadersProvider,
				logger,
				true)
		{
			this.gitHubServiceFactory = gitHubServiceFactory ?? throw new ArgumentNullException(nameof(gitHubServiceFactory));
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			this.serverUpdateInitiator = serverUpdateInitiator ?? throw new ArgumentNullException(nameof(serverUpdateInitiator));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
			this.cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
			fileLoggingConfiguration = fileLoggingConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(fileLoggingConfigurationOptions));
		}

		/// <summary>
		/// Get <see cref="AdministrationResponse"/> server information.
		/// </summary>
		/// <param name="fresh">If <see langword="true"/>, the cache should be bypassed.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Retrieved <see cref="AdministrationResponse"/> data successfully.</response>
		/// <response code="424">The GitHub API rate limit was hit. See response header Retry-After.</response>
		/// <response code="429">A GitHub API error occurred. See error message for details.</response>
		[HttpGet]
		[TgsAuthorize(AdministrationRights.ChangeVersion)]
		[ProducesResponseType(typeof(AdministrationResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 424)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 429)]
		public async ValueTask<IActionResult> Read([FromQuery] bool? fresh, CancellationToken cancellationToken)
		{
			try
			{
				async Task<JsonResult> CacheFactory()
				{
					Version? greatestVersion = null;
					Uri? repoUrl = null;
					try
					{
						var gitHubService = await gitHubServiceFactory.CreateService(cancellationToken);
						var repositoryUrlTask = gitHubService.GetUpdatesRepositoryUrl(cancellationToken);
						var releases = await gitHubService.GetTgsReleases(cancellationToken);

						foreach (var kvp in releases)
						{
							var version = kvp.Key;
							var release = kvp.Value;
							if (version.Major > 3 // Forward/backward compatible but not before TGS4
								&& (greatestVersion == null || version > greatestVersion))
								greatestVersion = version;
						}

						repoUrl = await repositoryUrlTask;
					}
					catch (NotFoundException e)
					{
						Logger.LogWarning(e, "Not found exception while retrieving upstream repository info!");
					}

					return Json(new AdministrationResponse
					{
						LatestVersion = greatestVersion,
						TrackedRepositoryUrl = repoUrl,
						GeneratedAt = DateTimeOffset.UtcNow,
					});
				}

				var ttl = TimeSpan.FromMinutes(30);
				Task<JsonResult> task;
				if (fresh == true || !cacheService.TryGetValue(ReadCacheKey, out var rawCacheObject))
				{
					using var entry = cacheService.CreateEntry(ReadCacheKey);
					entry.AbsoluteExpirationRelativeToNow = ttl;
					entry.Value = task = CacheFactory();
				}
				else
					task = (Task<JsonResult>)rawCacheObject!;

				return await task;
			}
			catch (RateLimitExceededException e)
			{
				return RateLimit(e);
			}
			catch (ApiException e)
			{
				Logger.LogWarning(e, OctokitException);
				return this.StatusCode(HttpStatusCode.FailedDependency, new ErrorMessageResponse(ErrorCode.RemoteApiError)
				{
					AdditionalData = e.Message,
				});
			}
		}

		/// <summary>
		/// Attempt to perform a server upgrade.
		/// </summary>
		/// <param name="model">The <see cref="ServerUpdateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="202">Update has been started successfully.</response>
		/// <response code="410">The requested release version could not be found in the target GitHub repository.</response>
		/// <response code="422">Upgrade operations are unavailable due to the launch configuration of TGS.</response>
		/// <response code="424">A GitHub rate limit was encountered or the swarm integrity check failed.</response>
		/// <response code="429">A GitHub API error occurred.</response>
		[HttpPost]
		[TgsAuthorize(AdministrationRights.ChangeVersion | AdministrationRights.UploadVersion)]
		[ProducesResponseType(typeof(ServerUpdateResponse), 202)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 422)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 424)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 429)]
		public async ValueTask<IActionResult> Update([FromBody] ServerUpdateRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);

			var attemptingUpload = model.UploadZip == true;
			if (attemptingUpload)
			{
				if (!AuthenticationContext.PermissionSet.AdministrationRights!.Value.HasFlag(AdministrationRights.UploadVersion))
					return Forbid();
			}
			else if (!AuthenticationContext.PermissionSet.AdministrationRights!.Value.HasFlag(AdministrationRights.ChangeVersion))
				return Forbid();

			if (model.NewVersion == null)
				return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure)
				{
					AdditionalData = "newVersion is required!",
				});

			if (model.NewVersion.Major < 3)
				return BadRequest(new ErrorMessageResponse(ErrorCode.CannotChangeServerSuite));

			if (!serverControl.WatchdogPresent)
				return UnprocessableEntity(new ErrorMessageResponse(ErrorCode.MissingHostWatchdog));

			return await AttemptInitiateUpdate(model.NewVersion, attemptingUpload, cancellationToken);
		}

		/// <summary>
		/// Attempts to restart the server.
		/// </summary>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="204">Restart begun successfully.</response>
		/// <response code="422">Restart operations are unavailable due to the launch configuration of TGS.</response>
		[HttpDelete]
		[TgsAuthorize(AdministrationRights.RestartHost)]
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 422)]
		public async ValueTask<IActionResult> Delete()
		{
			try
			{
				if (!serverControl.WatchdogPresent)
				{
					Logger.LogDebug("Restart request failed due to lack of host watchdog!");
					return UnprocessableEntity(new ErrorMessageResponse(ErrorCode.MissingHostWatchdog));
				}

				await serverControl.Restart();
				return NoContent();
			}
			catch (InvalidOperationException)
			{
				return StatusCode(HttpStatusCode.ServiceUnavailable);
			}
		}

		/// <summary>
		/// List <see cref="LogFileResponse"/>s present.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Listed logs successfully.</response>
		/// <response code="409">An IO error occurred while listing.</response>
		[HttpGet(Routes.Logs)]
		[TgsAuthorize(AdministrationRights.DownloadLogs)]
		[ProducesResponseType(typeof(PaginatedResponse<LogFileResponse>), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 409)]
		public ValueTask<IActionResult> ListLogs([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> Paginated(
				async () =>
				{
					var path = fileLoggingConfiguration.GetFullLogDirectory(ioManager, assemblyInformationProvider, platformIdentifier);
					try
					{
						var files = await ioManager.GetFiles(path, cancellationToken);
						var tasks = files.Select(
							async file => new LogFileResponse
							{
								Name = ioManager.GetFileName(file),
								LastModified = await ioManager
									.GetLastModified(
										ioManager.ConcatPath(path, file),
										cancellationToken),
							})
							.ToList();

						await Task.WhenAll(tasks);

						return new PaginatableResult<LogFileResponse>(
							tasks
								.AsQueryable()
								.Select(x => x.Result)
								.OrderByDescending(x => x.Name));
					}
					catch (IOException ex)
					{
						return new PaginatableResult<LogFileResponse>(
							Conflict(new ErrorMessageResponse(ErrorCode.IOError)
							{
								AdditionalData = ex.ToString(),
							}));
					}
				},
				null,
				page,
				pageSize,
				cancellationToken);

		/// <summary>
		/// Download a <see cref="LogFileResponse"/>.
		/// </summary>
		/// <param name="path">The path to download.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Downloaded <see cref="LogFileResponse"/> successfully.</response>
		/// <response code="409">An IO error occurred while downloading.</response>
		[HttpGet(Routes.Logs + "/{*path}")]
		[TgsAuthorize(AdministrationRights.DownloadLogs)]
		[ProducesResponseType(typeof(LogFileResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 409)]
		public async ValueTask<IActionResult> GetLog(string path, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(path);

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
				var fileTransferTicket = fileTransferService.CreateDownload(
					new FileDownloadProvider(
						() => null,
						null,
						fullPath,
						true));

				return Ok(new LogFileResponse
				{
					Name = path,
					LastModified = await ioManager.GetLastModified(fullPath, cancellationToken),
					FileTicket = fileTransferTicket.FileTicket,
				});
			}
			catch (IOException ex)
			{
				return Conflict(new ErrorMessageResponse(ErrorCode.IOError)
				{
					AdditionalData = ex.ToString(),
				});
			}
		}

		/// <summary>
		/// Attempt to initiate an update.
		/// </summary>
		/// <param name="newVersion">The <see cref="Version"/> being updated to.</param>
		/// <param name="attemptingUpload">If an upload is being attempted.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		async ValueTask<IActionResult> AttemptInitiateUpdate(Version newVersion, bool attemptingUpload, CancellationToken cancellationToken)
		{
			IFileUploadTicket? uploadTicket = attemptingUpload
				? fileTransferService.CreateUpload(FileUploadStreamKind.None)
				: null;

			ServerUpdateResult updateResult;
			try
			{
				try
				{
					updateResult = await serverUpdateInitiator.InitiateUpdate(uploadTicket, newVersion, cancellationToken);
				}
				catch
				{
					if (attemptingUpload)
						await uploadTicket!.DisposeAsync();

					throw;
				}
			}
			catch (RateLimitExceededException e)
			{
				return RateLimit(e);
			}
			catch (ApiException e)
			{
				Logger.LogWarning(e, OctokitException);
				return this.StatusCode(HttpStatusCode.FailedDependency, new ErrorMessageResponse(ErrorCode.RemoteApiError)
				{
					AdditionalData = e.Message,
				});
			}

			return updateResult switch
			{
				ServerUpdateResult.Started => Accepted(new ServerUpdateResponse(newVersion, uploadTicket?.Ticket.FileTicket)),
				ServerUpdateResult.ReleaseMissing => this.Gone(),
				ServerUpdateResult.UpdateInProgress => BadRequest(new ErrorMessageResponse(ErrorCode.ServerUpdateInProgress)),
				ServerUpdateResult.SwarmIntegrityCheckFailed => this.StatusCode(HttpStatusCode.FailedDependency, new ErrorMessageResponse(ErrorCode.SwarmIntegrityCheckFailed)),
				_ => throw new InvalidOperationException($"Unexpected ServerUpdateResult: {updateResult}"),
			};
		}
	}
}
