using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Database;
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
		/// Remove the <see cref="Version.Build"/> from a given <paramref name="version"/> if present.
		/// </summary>
		/// <param name="version">The <see cref="Version"/> to normalize.</param>
		/// <returns>The normalized <see cref="Version"/>. May be a reference to <paramref name="version"/>.</returns>
		static Version NormalizeVersion(Version version) => version.Build == 0 ? new Version(version.Major, version.Minor) : version;

		/// <summary>
		/// Initializes a new instance of the <see cref="ByondController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="fileTransferService">The value of <see cref="fileTransferService"/>.</param>
		public ByondController(
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			ILogger<ByondController> logger,
			IInstanceManager instanceManager,
			IJobManager jobManager,
			IFileTransferTicketProvider fileTransferService)
			: base(
				  databaseContext,
				  authenticationContextFactory,
				  logger,
				  instanceManager)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
		}

		/// <summary>
		/// Gets the active <see cref="ByondResponse.Version"/>.
		/// </summary>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Retrieved version information successfully.</response>
		[HttpGet]
		[TgsAuthorize(ByondRights.ReadActive)]
		[ProducesResponseType(typeof(ByondResponse), 200)]
		public Task<IActionResult> Read()
			=> WithComponentInstance(instance =>
				Task.FromResult<IActionResult>(
					Json(new ByondResponse
					{
						Version = instance.ByondManager.ActiveVersion,
					})));

		/// <summary>
		/// Lists installed <see cref="ByondResponse.Version"/>s.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Retrieved version information successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(ByondRights.ListInstalled)]
		[ProducesResponseType(typeof(PaginatedResponse<ByondResponse>), 200)]
		public Task<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> WithComponentInstance(
				instance => Paginated(
					() => Task.FromResult(
						new PaginatableResult<ByondResponse>(
							instance
								.ByondManager
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
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Switched active version successfully.</response>
		/// <response code="202">Created <see cref="Job"/> to install and switch active version successfully.</response>
		[HttpPost]
		[TgsAuthorize(ByondRights.InstallOfficialOrChangeActiveVersion | ByondRights.InstallCustomVersion)]
		[ProducesResponseType(typeof(ByondInstallResponse), 200)]
		[ProducesResponseType(typeof(ByondInstallResponse), 202)]
#pragma warning disable CA1506 // TODO: Decomplexify
		public async Task<IActionResult> Update([FromBody] ByondVersionRequest model, CancellationToken cancellationToken)
#pragma warning restore CA1506
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			var uploadingZip = model.UploadCustomZip == true;

			if (model.Version == null
				|| model.Version.Revision != -1
				|| (uploadingZip && model.Version.Build > 0))
				return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));

			var version = NormalizeVersion(model.Version);

			var userByondRights = AuthenticationContext.InstancePermissionSet.ByondRights.Value;
			if ((!userByondRights.HasFlag(ByondRights.InstallOfficialOrChangeActiveVersion) && !uploadingZip)
				|| (!userByondRights.HasFlag(ByondRights.InstallCustomVersion) && uploadingZip))
				return Forbid();

			// remove cruff fields
			var result = new ByondInstallResponse();
			return await WithComponentInstance(
				async instance =>
				{
					var byondManager = instance.ByondManager;
					var versionAlreadyInstalled = !uploadingZip && byondManager.InstalledVersions.Any(x => x == version);
					if (versionAlreadyInstalled)
					{
						Logger.LogInformation(
							"User ID {userId} changing instance ID {instanceId} BYOND version to {newByondVersion}",
							AuthenticationContext.User.Id,
							Instance.Id,
							version);

						try
						{
							await byondManager.ChangeVersion(null, version, null, false, cancellationToken);
						}
						catch (InvalidOperationException ex)
						{
							Logger.LogDebug(
								ex,
								"Race condition: BYOND version {version} uninstalled before we could switch to it. Creating install job instead...",
								version);
							versionAlreadyInstalled = false;
						}
					}

					if (!versionAlreadyInstalled)
					{
						if (version.Build > 0)
							return BadRequest(new ErrorMessageResponse(ErrorCode.ByondNonExistentCustomVersion));

						Logger.LogInformation(
							"User ID {userId} installing BYOND version to {newByondVersion} on instance ID {instanceId}",
							AuthenticationContext.User.Id,
							version,
							Instance.Id);

						// run the install through the job manager
						var job = new Job
						{
							Description = $"Install {(!uploadingZip ? String.Empty : "custom ")}BYOND version {version}",
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
									Stream zipFileStream = null;
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
										await core.ByondManager.ChangeVersion(
											progressHandler,
											version,
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
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="202">Created <see cref="Job"/> to delete target version successfully.</response>
		/// <response code="409">Attempted to delete the active BYOND <see cref="Version"/>.</response>
		/// <response code="410">The <see cref="ByondVersionDeleteRequest.Version"/> specified was not installed.</response>
		[HttpDelete]
		[TgsAuthorize(ByondRights.DeleteInstall)]
		[ProducesResponseType(typeof(JobResponse), 202)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 409)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public async Task<IActionResult> Delete([FromBody] ByondVersionDeleteRequest model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.Version == null
				|| model.Version.Revision != -1)
				return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));

			var version = NormalizeVersion(model.Version);

			var notInstalledResponse = await WithComponentInstance(
				instance =>
				{
					var byondManager = instance.ByondManager;

					if (version == byondManager.ActiveVersion)
						return Task.FromResult<IActionResult>(
							Conflict(new ErrorMessageResponse(ErrorCode.ByondCannotDeleteActiveVersion)));

					var versionNotInstalled = !byondManager.InstalledVersions.Any(x => x == version);

					return Task.FromResult<IActionResult>(
						versionNotInstalled
							? Gone()
							: null);
				});

			if (notInstalledResponse != null)
				return notInstalledResponse;

			var isCustomVersion = version.Build != -1;

			// run the install through the job manager
			var job = new Job
			{
				Description = $"Delete installed BYOND version {version}",
				StartedBy = AuthenticationContext.User,
				CancelRightsType = RightsType.Byond,
				CancelRight = (ulong)(isCustomVersion ? ByondRights.InstallOfficialOrChangeActiveVersion : ByondRights.InstallCustomVersion),
				Instance = Instance,
			};

			await jobManager.RegisterOperation(
				job,
				(instanceCore, databaseContextFactory, job, progressReporter, jobCancellationToken)
					=> instanceCore.ByondManager.DeleteVersion(progressReporter, version, jobCancellationToken),
				cancellationToken);

			var apiResponse = job.ToApi();
			return Accepted(apiResponse);
		}
	}
}
