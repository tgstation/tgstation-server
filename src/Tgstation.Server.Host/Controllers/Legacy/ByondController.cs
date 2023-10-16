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
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Controllers.Legacy.Models;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Transfer;

namespace Tgstation.Server.Host.Controllers.Legacy
{
	/// <summary>
	/// Controller for managing BYOND installations.
	/// </summary>
	[Route(Routes.Root + "Byond")]
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
		/// Create an <see cref="EngineVersion"/> for a given legacy formatted BYOND <paramref name="version"/>.
		/// </summary>
		/// <param name="version">The legacy BYOND <see cref="Version"/>.</param>
		/// <returns>A <see cref="EngineType.Byond"/> <see cref="EngineVersion"/> <paramref name="version"/>.</returns>
		static EngineVersion CreateEngineVersionFromLegacyByondVersion(Version version) => new EngineVersion
		{
			Version = new Version(version.Major, version.Minor),
			Engine = EngineType.Byond,
			CustomIteration = version.Build <= 0 ? null : version.Build,
		};

		/// <summary>
		/// Create a legacy formated BYOND <see cref="Version"/> for a given <paramref name="engineVersion"/>.
		/// </summary>
		/// <param name="engineVersion">The <see cref="EngineType.Byond"/> <see cref="EngineVersion"/>.</param>
		/// <returns>A legacy BYOND <see cref="Version"/>.</returns>
		static Version CreateLegacyByondVersionFromEngineVersion(EngineVersion engineVersion)
			=> new (engineVersion.Version.Major, engineVersion.Version.Minor, engineVersion.CustomIteration ?? 0);

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
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Retrieved version information successfully.</response>
		[HttpGet]
		[TgsAuthorize(EngineRights.ReadActive)]
		[ProducesResponseType(typeof(ByondResponse), 200)]
		public ValueTask<IActionResult> Read()
			=> WithComponentInstance(instance =>
			{
				var version = instance.EngineManager.ActiveVersion;
				return ValueTask.FromResult<IActionResult>(
					Json(new ByondResponse
					{
						Version = version?.Engine.Value == EngineType.Byond
							? CreateLegacyByondVersionFromEngineVersion(version)
							: null,
					}));
			});

		/// <summary>
		/// Lists installed <see cref="ByondResponse.Version"/>s.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Retrieved version information successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(EngineRights.ListInstalled)]
		[ProducesResponseType(typeof(PaginatedResponse<ByondResponse>), 200)]
		public ValueTask<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> WithComponentInstance(
				instance => Paginated(
					() => ValueTask.FromResult(
						new PaginatableResult<ByondResponse>(
							instance
								.EngineManager
								.InstalledVersions
								.Where(x => x.Engine.Value == EngineType.Byond)
								.Select(x => new ByondResponse
								{
									Version = CreateLegacyByondVersionFromEngineVersion(x),
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
		[TgsAuthorize(EngineRights.InstallOfficialOrChangeActiveByondVersion | EngineRights.InstallCustomByondVersion)]
		[ProducesResponseType(typeof(ByondInstallResponse), 200)]
		[ProducesResponseType(typeof(ByondInstallResponse), 202)]
#pragma warning disable CA1506 // TODO: Decomplexify
		public async ValueTask<IActionResult> Update([FromBody] ByondVersionRequest model, CancellationToken cancellationToken)
#pragma warning restore CA1506
		{
			ArgumentNullException.ThrowIfNull(model);

			var uploadingZip = model.UploadCustomZip == true;

			if (model.Version == null
				|| model.Version.Revision != -1
				|| (uploadingZip && model.Version.Build > 0))
				return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));

			var userEngineRights = AuthenticationContext.InstancePermissionSet.EngineRights.Value;
			if ((!userEngineRights.HasFlag(EngineRights.InstallOfficialOrChangeActiveByondVersion) && !uploadingZip)
				|| (!userEngineRights.HasFlag(EngineRights.InstallCustomByondVersion) && uploadingZip))
				return Forbid();

			// remove cruff fields
			var result = new ByondInstallResponse();
			return await WithComponentInstance(
				async instance =>
				{
					var byondManager = instance.EngineManager;
					var engineVersion = CreateEngineVersionFromLegacyByondVersion(model.Version);
					var versionAlreadyInstalled = !uploadingZip
						&& byondManager
							.InstalledVersions
							.Any(x => x.Equals(engineVersion));
					if (versionAlreadyInstalled)
					{
						Logger.LogInformation(
							"User ID {userId} changing instance ID {instanceId} BYOND version to {newByondVersion}",
							AuthenticationContext.User.Id,
							Instance.Id,
							engineVersion);

						try
						{
							await byondManager.ChangeVersion(
								null,
								engineVersion,
								null,
								false,
								cancellationToken);
						}
						catch (InvalidOperationException ex)
						{
							Logger.LogDebug(
								ex,
								"Race condition: BYOND version {version} uninstalled before we could switch to it. Creating install job instead...",
								engineVersion);
							versionAlreadyInstalled = false;
						}
					}

					if (!versionAlreadyInstalled)
					{
						if (engineVersion.CustomIteration.HasValue)
							return BadRequest(new ErrorMessageResponse(ErrorCode.EngineNonExistentCustomVersion));

						Logger.LogInformation(
							"User ID {userId} installing BYOND version to {newByondVersion} on instance ID {instanceId}",
							AuthenticationContext.User.Id,
							engineVersion,
							Instance.Id);

						// run the install through the job manager
						var job = new Host.Models.Job
						{
							Description = $"Install {(!uploadingZip ? string.Empty : "custom ")}BYOND version {engineVersion}",
							StartedBy = AuthenticationContext.User,
							CancelRightsType = RightsType.Engine,
							CancelRight = (ulong)EngineRights.CancelInstall,
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
										await core.EngineManager.ChangeVersion(
											progressHandler,
											engineVersion,
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
		/// <response code="410">The <see cref="ByondVersionDeleteRequest.Version"/> specified was not installed.</response>
		[HttpDelete]
		[TgsAuthorize(EngineRights.DeleteInstall)]
		[ProducesResponseType(typeof(JobResponse), 202)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 409)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public async ValueTask<IActionResult> Delete([FromBody] ByondVersionDeleteRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);

			if (model.Version == null
				|| model.Version.Revision != -1)
				return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));

			var engineVersion = CreateEngineVersionFromLegacyByondVersion(model.Version);
			var notInstalledResponse = await WithComponentInstance(
				instance =>
				{
					var engineManager = instance.EngineManager;

					if (engineVersion.Equals(engineManager.ActiveVersion))
						return ValueTask.FromResult<IActionResult>(
							Conflict(new ErrorMessageResponse(ErrorCode.EngineCannotDeleteActiveVersion)));

					var versionNotInstalled = !engineManager.InstalledVersions.Any(x => x.Equals(engineVersion));

					return ValueTask.FromResult<IActionResult>(
						versionNotInstalled
							? this.Gone()
							: null);
				});

			if (notInstalledResponse != null)
				return notInstalledResponse;

			// run the install through the job manager
			var job = new Host.Models.Job
			{
				Description = $"Delete installed BYOND version {engineVersion}",
				StartedBy = AuthenticationContext.User,
				CancelRightsType = RightsType.Engine,
				CancelRight = (ulong)(engineVersion.CustomIteration.HasValue ? EngineRights.InstallOfficialOrChangeActiveByondVersion : EngineRights.InstallCustomByondVersion),
				Instance = Instance,
			};

			await jobManager.RegisterOperation(
				job,
				(instanceCore, databaseContextFactory, job, progressReporter, jobCancellationToken)
					=> instanceCore.EngineManager.DeleteVersion(progressReporter, engineVersion, jobCancellationToken),
				cancellationToken);

			var apiResponse = job.ToApi();
			return Accepted(apiResponse);
		}
	}
}
