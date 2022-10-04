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
		/// Initializes a new instance of the <see cref="ByondController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/>.</param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="fileTransferService">The value of <see cref="fileTransferService"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/>.</param>
		public ByondController(
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			IInstanceManager instanceManager,
			IJobManager jobManager,
			IFileTransferTicketProvider fileTransferService,
			ILogger<ByondController> logger)
			: base(
				  instanceManager,
				  databaseContext,
				  authenticationContextFactory,
				  logger)
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
		/// <param name="model">The <see cref="ByondVersionRequest.Version"/> to switch to.</param>
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
					if (!uploadingZip && byondManager.InstalledVersions.Any(x => x == model.Version))
					{
						Logger.LogInformation(
							"User ID {userId} changing instance ID {instanceId} BYOND version to {newByondVersion}",
							AuthenticationContext.User.Id,
							Instance.Id,
							model.Version);
						await byondManager.ChangeVersion(model.Version, null, cancellationToken);
					}
					else if (model.Version.Build > 0)
						return BadRequest(new ErrorMessageResponse(ErrorCode.ByondNonExistentCustomVersion));
					else
					{
						var installingVersion = model.Version.Build <= 0
							? new Version(model.Version.Major, model.Version.Minor)
							: model.Version;

						Logger.LogInformation(
							"User ID {userId} installing BYOND version to {newByondVersion} on instance ID {instanceId}",
							AuthenticationContext.User.Id,
							installingVersion,
							Instance.Id);

						// run the install through the job manager
						var job = new Job
						{
							Description = $"Install {(!uploadingZip ? String.Empty : "custom ")}BYOND version {model.Version.Major}.{model.Version.Minor}",
							StartedBy = AuthenticationContext.User,
							CancelRightsType = RightsType.Byond,
							CancelRight = (ulong)ByondRights.CancelInstall,
							Instance = Instance,
						};

						IFileUploadTicket fileUploadTicket = null;
						if (uploadingZip)
							fileUploadTicket = fileTransferService.CreateUpload(false);

						try
						{
							await jobManager.RegisterOperation(
								job,
								async (core, databaseContextFactory, paramJob, progressHandler, jobCancellationToken) =>
								{
									Stream zipFileStream = null;
									if (fileUploadTicket != null)
										using (fileUploadTicket)
										{
											var uploadStream = await fileUploadTicket.GetResult(jobCancellationToken);
											if (uploadStream == null)
												throw new JobException(ErrorCode.FileUploadExpired);

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

									using (zipFileStream)
										await core.ByondManager.ChangeVersion(
											model.Version,
											zipFileStream,
											jobCancellationToken)
										;
								},
								cancellationToken)
								;

							result.InstallJob = job.ToApi();
							result.FileTicket = fileUploadTicket?.Ticket.FileTicket;
						}
						catch
						{
							fileUploadTicket?.Dispose();
							throw;
						}
					}

					return result.InstallJob != null ? Accepted(result) : Json(result);
				})
				;
		}
	}
}
