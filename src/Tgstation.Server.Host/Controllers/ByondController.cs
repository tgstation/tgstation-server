using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Transfer;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Controller for managing BYOND installations.
	/// </summary>
	[Route(Routes.Byond)]
	public sealed class ByondController : InstanceRequiredController
	{
		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="ByondController"/>.
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="IFileTransferTicketProvider"/> for the <see cref="ByondController"/>.
		/// </summary>
		readonly IFileTransferTicketProvider fileTransferService;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="ByondController"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// Remove the <see cref="Version.Build"/> from a given <paramref name="version"/> if present.
		/// </summary>
		/// <param name="version">The <see cref="Version"/> to normalize.</param>
		/// <returns>The normalized <see cref="Version"/>. May be a reference to <paramref name="version"/>.</returns>
		static Version NormalizeByondVersion(Version version) => version.Build == 0 ? new Version(version.Major, version.Minor) : version;

		/// <summary>
		/// Initializes a new instance of the <see cref="ByondController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="fileTransferService">The value of <see cref="fileTransferService"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		public ByondController(
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			ILogger<ByondController> logger,
			IInstanceManager instanceManager,
			IJobManager jobManager,
			IFileTransferTicketProvider fileTransferService,
			IOptions<GeneralConfiguration> generalConfigurationOptions)
			: base(
				  databaseContext,
				  authenticationContextFactory,
				  logger,
				  instanceManager)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <summary>
		/// Gets the active <see cref="EngineVersion"/>.
		/// </summary>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Retrieved version information successfully.</response>
		/// <response code="409">No BYOND versions installed.</response>
		[HttpGet]
		[TgsAuthorize(ByondRights.ReadActive)]
		[ProducesResponseType(typeof(ByondResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 409)]
		public ValueTask<IActionResult> Read()
			=> WithComponentInstance(instance =>
				ValueTask.FromResult<IActionResult>(
					Json(
						new ByondResponse
						{
							Version = instance.EngineManager.ActiveVersion,
						})));

		/// <summary>
		/// Lists installed <see cref="EngineVersion"/>s.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Retrieved version information successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(ByondRights.ListInstalled)]
		[ProducesResponseType(typeof(PaginatedResponse<ByondResponse>), 200)]
		public ValueTask<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> WithComponentInstance(
				instance => Paginated(
					() => ValueTask.FromResult(
						new PaginatableResult<ByondResponse>(
							instance
								.EngineManager
								.InstalledVersions
								.Select(x => new ByondResponse
								{
									Version = x,
								})
								.AsQueryable()
								.OrderBy(x => x.Version))),
					null,
					page,
					pageSize,
					cancellationToken));

		/// <summary>
		/// Changes the active BYOND version to the one specified in a given <paramref name="model"/>.
		/// </summary>
		/// <param name="model">The <see cref="ByondVersionRequest"/> containing the <see cref="Version"/> to switch to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Switched active version successfully.</response>
		/// <response code="202">Created <see cref="Job"/> to install and switch active version successfully.</response>
		[HttpPost]
		[TgsAuthorize(
			ByondRights.InstallOfficialOrChangeActiveByondVersion
			| ByondRights.InstallCustomByondVersion
			| ByondRights.InstallOfficialOrChangeActiveOpenDreamVersion
			| ByondRights.InstallCustomOpenDreamVersion)]
		[ProducesResponseType(typeof(ByondInstallResponse), 200)]
		[ProducesResponseType(typeof(ByondInstallResponse), 202)]
#pragma warning disable CA1502 // TODO: Decomplexify
#pragma warning disable CA1506
		public async ValueTask<IActionResult> Update([FromBody] ByondVersionRequest model, CancellationToken cancellationToken)
#pragma warning restore CA1506
#pragma warning restore CA1502
		{
			var earlyOut = ValidateByondVersion(model);
			if (earlyOut != null)
				return earlyOut;

			var uploadingZip = model.UploadCustomZip == true;

			var userByondRights = AuthenticationContext.InstancePermissionSet.ByondRights.Value;
			if ((!userByondRights.HasFlag(ByondRights.InstallOfficialOrChangeActiveByondVersion) && !uploadingZip)
				|| (!userByondRights.HasFlag(ByondRights.InstallCustomByondVersion) && uploadingZip))
				return Forbid();

			// remove cruff fields
			var result = new ByondInstallResponse();
			return await WithComponentInstance(
				async instance =>
				{
					var byondManager = instance.EngineManager;
					var versionAlreadyInstalled = !uploadingZip && byondManager.InstalledVersions.Any(x => x.Equals(model));
					if (versionAlreadyInstalled)
					{
						Logger.LogInformation(
							"User ID {userId} changing instance ID {instanceId} engine to {newByondVersion}",
							AuthenticationContext.User.Id,
							Instance.Id,
							model);

						try
						{
							await byondManager.ChangeVersion(null, model, null, false, cancellationToken);
						}
						catch (InvalidOperationException ex)
						{
							Logger.LogDebug(
								ex,
								"Race condition: Engine {version} uninstalled before we could switch to it. Creating install job instead...",
								model);
							versionAlreadyInstalled = false;
						}
					}

					if (!versionAlreadyInstalled)
					{
						if (model.CustomIteration.HasValue)
							return BadRequest(new ErrorMessageResponse(ErrorCode.EngineNonExistentCustomVersion));

						Logger.LogInformation(
							"User ID {userId} installing engine version {newByondVersion} on instance ID {instanceId}",
							AuthenticationContext.User.Id,
							model,
							Instance.Id);

						// run the install through the job manager
						var job = new Models.Job
						{
							Description = $"Install {(!uploadingZip ? String.Empty : "custom ")}{model.Engine.Value} version {model.Version}",
							StartedBy = AuthenticationContext.User,
							CancelRightsType = RightsType.Byond,
							CancelRight = (ulong)ByondRights.CancelInstall,
							Instance = Instance,
						};

						IFileUploadTicket fileUploadTicket = null;
						if (uploadingZip)
							fileUploadTicket = fileTransferService.CreateUpload(FileUploadStreamKind.None);

						try
						{
							await jobManager.RegisterOperation(
								job,
								async (core, databaseContextFactory, paramJob, progressHandler, jobCancellationToken) =>
								{
									MemoryStream zipFileStream = null;
									if (fileUploadTicket != null)
										await using (fileUploadTicket)
										{
											var uploadStream = await fileUploadTicket.GetResult(jobCancellationToken) ?? throw new JobException(ErrorCode.FileUploadExpired);
											zipFileStream = new MemoryStream();
											try
											{
												await uploadStream.CopyToAsync(zipFileStream, jobCancellationToken);
											}
											catch
											{
												await zipFileStream.DisposeAsync();
												throw;
											}
										}

									await using (zipFileStream)
										await core.EngineManager.ChangeVersion(
											progressHandler,
											model,
											zipFileStream,
											true,
											jobCancellationToken);
								},
								cancellationToken);

							result.InstallJob = job.ToApi();
							result.FileTicket = fileUploadTicket?.Ticket.FileTicket;
						}
						catch
						{
							if (fileUploadTicket != null)
								await fileUploadTicket.DisposeAsync();

							throw;
						}
					}

					return result.InstallJob != null ? Accepted(result) : Json(result);
				});
		}

		/// <summary>
		/// Attempts to delete the BYOND version specified in a given <paramref name="model"/> from the instance.
		/// </summary>
		/// <param name="model">The <see cref="ByondVersionDeleteRequest"/> containing the <see cref="Version"/> to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="202">Created <see cref="Job"/> to delete target version successfully.</response>
		/// <response code="409">Attempted to delete the active BYOND <see cref="Version"/>.</response>
		/// <response code="410">The <see cref="EngineVersion"/> specified was not installed.</response>
		[HttpDelete]
		[TgsAuthorize(ByondRights.DeleteInstall)]
		[ProducesResponseType(typeof(JobResponse), 202)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 409)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public async ValueTask<IActionResult> Delete([FromBody] ByondVersionDeleteRequest model, CancellationToken cancellationToken)
		{
			var earlyOut = ValidateByondVersion(model);
			if (earlyOut != null)
				return earlyOut;

			var notInstalledResponse = await WithComponentInstance(
				instance =>
				{
					var byondManager = instance.EngineManager;

					if (model.Equals(byondManager.ActiveVersion))
						return ValueTask.FromResult<IActionResult>(
							Conflict(new ErrorMessageResponse(ErrorCode.EngineCannotDeleteActiveVersion)));

					var versionNotInstalled = !byondManager.InstalledVersions.Any(x => x.Equals(model));

					return ValueTask.FromResult<IActionResult>(
						versionNotInstalled
							? this.Gone()
							: null);
				});

			if (notInstalledResponse != null)
				return notInstalledResponse;

			var isByondVersion = model.Engine.Value == EngineType.Byond;

			// run the install through the job manager
			var job = new Models.Job
			{
				Description = $"Delete installed engine version {model}",
				StartedBy = AuthenticationContext.User,
				CancelRightsType = RightsType.Byond,
				CancelRight = (ulong)(
					isByondVersion
						? model.Version.Build != -1
							? ByondRights.InstallOfficialOrChangeActiveByondVersion
							: ByondRights.InstallCustomByondVersion
						: ByondRights.InstallCustomOpenDreamVersion | ByondRights.InstallOfficialOrChangeActiveOpenDreamVersion),
				Instance = Instance,
			};

			await jobManager.RegisterOperation(
				job,
				(instanceCore, databaseContextFactory, job, progressReporter, jobCancellationToken)
					=> instanceCore.EngineManager.DeleteVersion(progressReporter, model, jobCancellationToken),
				cancellationToken);

			var apiResponse = job.ToApi();
			return Accepted(apiResponse);
		}

		/// <summary>
		/// Validate and normalize a given <paramref name="version"/>.
		/// </summary>
		/// <param name="version">The <see cref="EngineVersion"/> to validate and normalize.</param>
		/// <returns>The <see cref="BadRequestObjectResult"/> to return, if any.</returns>
		BadRequestObjectResult ValidateByondVersion(EngineVersion version)
		{
			ArgumentNullException.ThrowIfNull(version);

			if (!version.Engine.HasValue)
				return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));

			var isByond = version.Engine.Value == EngineType.Byond;
			var validSha = version.SourceSHA?.Length == Limits.MaximumCommitShaLength;
			if ((isByond
				&& (version.Version == null
				|| validSha))
				|| (version.Engine.Value == EngineType.OpenDream &&
				((version.SourceSHA == null && version.Version == null)
				|| (version.Version != null && (version.Version.Revision != -1 || validSha)))))
				return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));

			if (isByond)
			{
				version.Version = NormalizeByondVersion(version.Version);
				if (version.Version.Build != -1)
					return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));
			}

			return null;
		}
	}
}
