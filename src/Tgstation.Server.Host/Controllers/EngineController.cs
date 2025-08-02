using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Controllers.Results;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Transfer;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Controller for managing engine installations.
	/// </summary>
	[Route(Routes.Engine)]
	public sealed class EngineController : InstanceRequiredController
	{
		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="EngineController"/>.
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="IFileTransferTicketProvider"/> for the <see cref="EngineController"/>.
		/// </summary>
		readonly IFileTransferTicketProvider fileTransferService;

		/// <summary>
		/// Remove the <see cref="Version.Build"/> from a given <paramref name="version"/> if present.
		/// </summary>
		/// <param name="version">The <see cref="Version"/> to normalize.</param>
		/// <returns>The normalized <see cref="Version"/>. May be a reference to <paramref name="version"/>.</returns>
		static Version NormalizeByondVersion(Version version) => version.Build == 0 ? new Version(version.Major, version.Minor) : version;

		/// <summary>
		/// Initializes a new instance of the <see cref="EngineController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="fileTransferService">The value of <see cref="fileTransferService"/>.</param>
		/// <param name="apiHeadersProvider">The <see cref="IApiHeadersProvider"/> for the <see cref="InstanceRequiredController"/>.</param>
		public EngineController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			ILogger<EngineController> logger,
			IInstanceManager instanceManager,
			IJobManager jobManager,
			IFileTransferTicketProvider fileTransferService,
			IApiHeadersProvider apiHeadersProvider)
			: base(
				  databaseContext,
				  authenticationContext,
				  logger,
				  instanceManager,
				  apiHeadersProvider)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
		}

		/// <summary>
		/// Gets the active <see cref="EngineVersion"/>.
		/// </summary>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Retrieved version information successfully.</response>
		/// <response code="409">No engine versions installed.</response>
		[HttpGet]
		[TgsAuthorize(EngineRights.ReadActive)]
		[ProducesResponseType(typeof(EngineResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 409)]
		public ValueTask<IActionResult> Read()
			=> WithComponentInstance(instance =>
				ValueTask.FromResult<IActionResult>(
					Json(
						new EngineResponse
						{
							EngineVersion = instance.EngineManager.ActiveVersion,
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
		[TgsAuthorize(EngineRights.ListInstalled)]
		[ProducesResponseType(typeof(PaginatedResponse<EngineResponse>), 200)]
		public ValueTask<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> WithComponentInstance(
				instance => Paginated(
					() => ValueTask.FromResult<PaginatableResult<EngineResponse>?>(
						new PaginatableResult<EngineResponse>(
							instance
								.EngineManager
								.InstalledVersions
								.Select(x => new EngineResponse
								{
									EngineVersion = x,
								})
								.AsQueryable()
								.OrderBy(x => x.EngineVersion!.ToString()))),
					null,
					page,
					pageSize,
					cancellationToken));

		/// <summary>
		/// Changes the active engine version to the one specified in a given <paramref name="model"/>.
		/// </summary>
		/// <param name="model">The <see cref="EngineVersionRequest"/> containing the <see cref="Version"/> to switch to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Switched active engine version successfully.</response>
		/// <response code="202">Created <see cref="Job"/> to install and switch active engine version successfully.</response>
		[HttpPost]
		[TgsAuthorize(
			EngineRights.InstallOfficialOrChangeActiveByondVersion
			| EngineRights.InstallCustomByondVersion
			| EngineRights.InstallOfficialOrChangeActiveOpenDreamVersion
			| EngineRights.InstallCustomOpenDreamVersion)]
		[ProducesResponseType(typeof(EngineInstallResponse), 200)]
		[ProducesResponseType(typeof(EngineInstallResponse), 202)]
#pragma warning disable CA1502 // TODO: Decomplexify
#pragma warning disable CA1506
		public async ValueTask<IActionResult> Update([FromBody] EngineVersionRequest model, CancellationToken cancellationToken)
#pragma warning restore CA1506
#pragma warning restore CA1502
		{
			ArgumentNullException.ThrowIfNull(model);
			var earlyOut = ValidateEngineVersion(model.EngineVersion);
			if (earlyOut != null)
				return earlyOut;

			var uploadingZip = model.UploadCustomZip == true;

			var engineRights = InstancePermissionSet.EngineRights!.Value;
			var isByondEngine = model.EngineVersion!.Engine!.Value == EngineType.Byond;
			var officialPerm = isByondEngine
				? EngineRights.InstallOfficialOrChangeActiveByondVersion
				: EngineRights.InstallOfficialOrChangeActiveOpenDreamVersion;
			var customPerm = isByondEngine
				? EngineRights.InstallCustomByondVersion
				: EngineRights.InstallCustomOpenDreamVersion;
			if ((!engineRights.HasFlag(officialPerm) && !uploadingZip)
				|| (!engineRights.HasFlag(customPerm) && uploadingZip))
				return Forbid();

			// remove cruff fields
			var result = new EngineInstallResponse();
			return await WithComponentInstance(
				async instance =>
				{
					var byondManager = instance.EngineManager;
					var versionAlreadyInstalled = !uploadingZip && byondManager.InstalledVersions.Any(x => x.Equals(model.EngineVersion));
					if (versionAlreadyInstalled)
					{
						Logger.LogInformation(
							"User ID {userId} changing instance ID {instanceId} engine to {newByondVersion}",
							AuthenticationContext.User.Id,
							Instance.Id,
							model);

						try
						{
							using var progressReporter = new JobProgressReporter();
							await byondManager.ChangeVersion(progressReporter, model.EngineVersion, null, false, cancellationToken);
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
						if (model.EngineVersion.CustomIteration.HasValue)
							return BadRequest(new ErrorMessageResponse(ErrorCode.EngineNonExistentCustomVersion));

						Logger.LogInformation(
							"User ID {userId} installing engine version {newByondVersion} on instance ID {instanceId}",
							AuthenticationContext.User.Id,
							model,
							Instance.Id);

						// run the install through the job manager
						var job = Models.Job.Create(
							uploadingZip
								? JobCode.EngineCustomInstall
								: JobCode.EngineOfficialInstall,
							AuthenticationContext.User,
							Instance,
							EngineRights.CancelInstall);
						job.Description += $" {model.EngineVersion}";

						IFileUploadTicket? fileUploadTicket = null;
						if (uploadingZip)
							fileUploadTicket = fileTransferService.CreateUpload(FileUploadStreamKind.None);

						try
						{
							await jobManager.RegisterOperation(
								job,
								async (core, databaseContextFactory, paramJob, progressHandler, jobCancellationToken) =>
								{
									MemoryStream? zipFileStream = null;
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
										await core!.EngineManager.ChangeVersion(
											progressHandler,
											model.EngineVersion,
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
		/// <param name="model">The <see cref="EngineVersionDeleteRequest"/> containing the <see cref="Version"/> to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="202">Created <see cref="Job"/> to delete target version successfully.</response>
		/// <response code="409">Attempted to delete the active BYOND <see cref="Version"/>.</response>
		/// <response code="410">The <see cref="EngineVersion"/> specified was not installed.</response>
		[HttpDelete]
		[TgsAuthorize(EngineRights.DeleteInstall)]
		[ProducesResponseType(typeof(JobResponse), 202)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 409)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public async ValueTask<IActionResult> Delete([FromBody] EngineVersionDeleteRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);
			var earlyOut = ValidateEngineVersion(model.EngineVersion);
			if (earlyOut != null)
				return earlyOut;

			var engineVersion = model.EngineVersion!;
			var notInstalledResponse = await WithComponentInstanceNullable(
				instance =>
				{
					var byondManager = instance.EngineManager;
					var activeVersion = byondManager.ActiveVersion;
					if (activeVersion != null && engineVersion.Equals(activeVersion))
						return ValueTask.FromResult<IActionResult?>(
							Conflict(new ErrorMessageResponse(ErrorCode.EngineCannotDeleteActiveVersion)));

					var versionNotInstalled = !byondManager.InstalledVersions.Any(x => x.Equals(engineVersion));

					return ValueTask.FromResult<IActionResult?>(
						versionNotInstalled
							? this.Gone()
							: null);
				});

			if (notInstalledResponse != null)
				return notInstalledResponse;

			var isByondVersion = engineVersion.Engine!.Value == EngineType.Byond;

			// run the install through the job manager
			var cancelRight = isByondVersion
				? engineVersion.CustomIteration.HasValue
					? EngineRights.InstallCustomByondVersion
					: EngineRights.InstallOfficialOrChangeActiveByondVersion
				: engineVersion.CustomIteration.HasValue
					? EngineRights.InstallOfficialOrChangeActiveOpenDreamVersion
					: EngineRights.InstallCustomOpenDreamVersion;

			var job = Models.Job.Create(JobCode.EngineDelete, AuthenticationContext.User, Instance, cancelRight);
			job.Description += $" {engineVersion}";

			await jobManager.RegisterOperation(
				job,
				(instanceCore, databaseContextFactory, job, progressReporter, jobCancellationToken)
					=> instanceCore!.EngineManager.DeleteVersion(progressReporter, engineVersion, jobCancellationToken),
				cancellationToken);

			var apiResponse = job.ToApi();
			return Accepted(apiResponse);
		}

		/// <summary>
		/// Validate and normalize a given <paramref name="version"/>.
		/// </summary>
		/// <param name="version">The <see cref="EngineVersion"/> to validate and normalize.</param>
		/// <returns>The <see cref="BadRequestObjectResult"/> to return, if any. <see langword="null"/> otherwise.</returns>
		BadRequestObjectResult? ValidateEngineVersion(EngineVersion? version)
		{
			if (version == null || !version.Engine.HasValue)
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
				version.Version = NormalizeByondVersion(version.Version!);
				if (version.Version.Build != -1)
					return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));
			}

			return null;
		}
	}
}
