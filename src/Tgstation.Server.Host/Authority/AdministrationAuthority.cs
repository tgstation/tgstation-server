using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Octokit;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Transfer;
using Tgstation.Server.Host.Utils.GitHub;

namespace Tgstation.Server.Host.Authority
{
	/// <inheritdoc cref="IAdministrationAuthority" />
	sealed class AdministrationAuthority : AuthorityBase, IAdministrationAuthority
	{
		/// <summary>
		/// Default <see cref="Exception.Message"/> for <see cref="ApiException"/>s.
		/// </summary>
		const string OctokitException = "Bad GitHub API response, check configuration!";

		/// <summary>
		/// The <see cref="IMemoryCache"/> key for <see cref="GetUpdateInformation(bool, CancellationToken)"/>.
		/// </summary>
		static readonly object ReadCacheKey = new();

		/// <summary>
		/// The <see cref="IGitHubServiceFactory"/> for the <see cref="AdministrationAuthority"/>.
		/// </summary>
		readonly IGitHubServiceFactory gitHubServiceFactory;

		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="AdministrationAuthority"/>.
		/// </summary>
		readonly IServerControl serverControl;

		/// <summary>
		/// The <see cref="IServerUpdateInitiator"/> for the <see cref="AdministrationAuthority"/>.
		/// </summary>
		readonly IServerUpdateInitiator serverUpdateInitiator;

		/// <summary>
		/// The <see cref="IFileTransferTicketProvider"/> for the <see cref="AdministrationAuthority"/>.
		/// </summary>
		readonly IFileTransferTicketProvider fileTransferService;

		/// <summary>
		/// The <see cref="IMemoryCache"/> for the <see cref="AdministrationAuthority"/>.
		/// </summary>
		readonly IMemoryCache cacheService;

		/// <summary>
		/// Initializes a new instance of the <see cref="AdministrationAuthority"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to use.</param>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		/// <param name="gitHubServiceFactory">The value of <see cref="gitHubServiceFactory"/>.</param>
		/// <param name="serverControl">The value of <see cref="serverControl"/>.</param>
		/// <param name="serverUpdateInitiator">The value of <see cref="serverUpdateInitiator"/>.</param>
		/// <param name="fileTransferService">The value of <see cref="fileTransferService"/>.</param>
		/// <param name="cacheService">The value of <see cref="cacheService"/>.</param>
		public AdministrationAuthority(
			IDatabaseContext databaseContext,
			ILogger<UserAuthority> logger,
			IGitHubServiceFactory gitHubServiceFactory,
			IServerControl serverControl,
			IServerUpdateInitiator serverUpdateInitiator,
			IFileTransferTicketProvider fileTransferService,
			IMemoryCache cacheService)
			: base(
				  databaseContext,
				  logger)
		{
			this.gitHubServiceFactory = gitHubServiceFactory ?? throw new ArgumentNullException(nameof(gitHubServiceFactory));
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			this.serverUpdateInitiator = serverUpdateInitiator ?? throw new ArgumentNullException(nameof(serverUpdateInitiator));
			this.fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
			this.cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
		}

		/// <inheritdoc />
		public RequirementsGated<AuthorityResponse<AdministrationResponse>> GetUpdateInformation(bool forceFresh, CancellationToken cancellationToken)
			=> new(
				() => Flag(AdministrationRights.ChangeVersion),
				async () =>
				{
					try
					{
						async Task<AdministrationResponse> CacheFactory()
						{
							Version? greatestVersion = null;
							Uri? repoUrl = null;
							var scopeCancellationToken = CancellationToken.None; // DCT: None available
							try
							{
								var gitHubService = await gitHubServiceFactory.CreateService(scopeCancellationToken);
								var repositoryUrlTask = gitHubService.GetUpdatesRepositoryUrl(scopeCancellationToken);
								var releases = await gitHubService.GetTgsReleases(scopeCancellationToken);

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

							return new AdministrationResponse
							{
								LatestVersion = greatestVersion,
								TrackedRepositoryUrl = repoUrl,
								GeneratedAt = DateTimeOffset.UtcNow,
							};
						}

						var ttl = TimeSpan.FromMinutes(30);
						Task<AdministrationResponse> task;
						if (forceFresh || !cacheService.TryGetValue(ReadCacheKey, out var rawCacheObject))
						{
							using var entry = cacheService.CreateEntry(ReadCacheKey);
							entry.AbsoluteExpirationRelativeToNow = ttl;
							entry.Value = task = CacheFactory();
						}
						else
							task = (Task<AdministrationResponse>)rawCacheObject!;

						var result = await task.WaitAsync(cancellationToken);
						return new AuthorityResponse<AdministrationResponse>(result);
					}
					catch (RateLimitExceededException e)
					{
						return RateLimit<AdministrationResponse>(e);
					}
					catch (ApiException e)
					{
						Logger.LogWarning(e, OctokitException);
						return new AuthorityResponse<AdministrationResponse>(
							new ErrorMessageResponse(ErrorCode.RemoteApiError)
							{
								AdditionalData = e.Message,
							},
							HttpFailureResponse.FailedDependency);
					}
				});

		/// <inheritdoc />
		public RequirementsGated<AuthorityResponse<ServerUpdateResponse>> TriggerServerVersionChange(Version targetVersion, bool uploadZip, CancellationToken cancellationToken)
			=> new(
				() =>
				{
					if (uploadZip)
						return Flag(AdministrationRights.UploadVersion);

					return Flag(AdministrationRights.ChangeVersion);
				},
				async () =>
				{
					if (targetVersion.Major < 4)
						return BadRequest<ServerUpdateResponse>(ErrorCode.CannotChangeServerSuite);

					if (!serverControl.WatchdogPresent)
						return new AuthorityResponse<ServerUpdateResponse>(
							new ErrorMessageResponse(ErrorCode.MissingHostWatchdog),
							HttpFailureResponse.UnprocessableEntity);

					IFileUploadTicket? uploadTicket = uploadZip
						? fileTransferService.CreateUpload(FileUploadStreamKind.None)
						: null;

					ServerUpdateResult updateResult;
					try
					{
						try
						{
							updateResult = await serverUpdateInitiator.InitiateUpdate(uploadTicket, targetVersion, cancellationToken);
						}
						catch
						{
							if (uploadZip)
								await uploadTicket!.DisposeAsync();

							throw;
						}
					}
					catch (RateLimitExceededException ex)
					{
						return RateLimit<ServerUpdateResponse>(ex);
					}
					catch (ApiException e)
					{
						Logger.LogWarning(e, OctokitException);
						return new AuthorityResponse<ServerUpdateResponse>(
							new ErrorMessageResponse(ErrorCode.RemoteApiError)
							{
								AdditionalData = e.Message,
							},
							HttpFailureResponse.FailedDependency);
					}

					return updateResult switch
					{
						ServerUpdateResult.Started => new AuthorityResponse<ServerUpdateResponse>(new ServerUpdateResponse(targetVersion, uploadTicket?.Ticket.FileTicket), HttpSuccessResponse.Accepted),
						ServerUpdateResult.ReleaseMissing => Gone<ServerUpdateResponse>(),
						ServerUpdateResult.UpdateInProgress => BadRequest<ServerUpdateResponse>(ErrorCode.ServerUpdateInProgress),
						ServerUpdateResult.SwarmIntegrityCheckFailed => new AuthorityResponse<ServerUpdateResponse>(
							new ErrorMessageResponse(ErrorCode.SwarmIntegrityCheckFailed),
							HttpFailureResponse.FailedDependency),
						_ => throw new InvalidOperationException($"Unexpected ServerUpdateResult: {updateResult}"),
					};
				});

		/// <inheritdoc />
		public RequirementsGated<AuthorityResponse> TriggerServerRestart()
			=> new(
				() => Flag(AdministrationRights.RestartHost),
				async () =>
				{
					if (!serverControl.WatchdogPresent)
					{
						Logger.LogDebug("Restart request failed due to lack of host watchdog!");
						return new AuthorityResponse(
							new ErrorMessageResponse(ErrorCode.MissingHostWatchdog),
							HttpFailureResponse.UnprocessableEntity);
					}

					await serverControl.Restart();
					return new AuthorityResponse();
				});
	}
}
