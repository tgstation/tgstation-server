using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
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
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing the deployment system.
	/// </summary>
	[Route(Routes.DreamMaker)]
	public sealed class DreamMakerController : InstanceRequiredController
	{
		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="DreamMakerController"/>.
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="IPortAllocator"/> for the <see cref="DreamMakerController"/>.
		/// </summary>
		readonly IPortAllocator portAllocator;

		/// <summary>
		/// Initializes a new instance of the <see cref="DreamMakerController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="portAllocator">The value of <see cref="IPortAllocator"/>.</param>
		/// <param name="apiHeaders">The <see cref="IApiHeadersProvider"/> for the <see cref="InstanceRequiredController"/>.</param>
		public DreamMakerController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			ILogger<DreamMakerController> logger,
			IInstanceManager instanceManager,
			IJobManager jobManager,
			IPortAllocator portAllocator,
			IApiHeadersProvider apiHeaders)
			: base(
				  databaseContext,
				  authenticationContext,
				  logger,
				  instanceManager,
				  apiHeaders)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.portAllocator = portAllocator ?? throw new ArgumentNullException(nameof(portAllocator));
		}

		/// <summary>
		/// Read current deployment settings.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Read deployment settings successfully.</response>
		[HttpGet]
		[TgsAuthorize(DreamMakerRights.Read)]
		[ProducesResponseType(typeof(DreamMakerResponse), 200)]
		public async ValueTask<IActionResult> Read(CancellationToken cancellationToken)
		{
			var dreamMakerSettings = await DatabaseContext
				.DreamMakerSettings
				.AsQueryable()
				.Where(x => x.InstanceId == Instance.Id)
				.FirstOrDefaultAsync(cancellationToken);

			if (dreamMakerSettings == null)
				return this.Gone();

			return Json(dreamMakerSettings.ToApi());
		}

		/// <summary>
		/// Get a <see cref="CompileJob"/> specified by a given <paramref name="id"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200"><see cref="CompileJob"/> retrieved successfully.</response>
		/// <response code="404">Specified <see cref="CompileJob"/> ID does not exist in this instance.</response>
		[HttpGet("{id}")]
		[TgsAuthorize(DreamMakerRights.CompileJobs)]
		[ProducesResponseType(typeof(CompileJobResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 404)]
		public async ValueTask<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			var compileJob = await BaseCompileJobsQuery()
				.Where(x => x.Id == id)
				.FirstOrDefaultAsync(cancellationToken);
			if (compileJob == default)
				return NotFound();
			return Json(compileJob.ToApi());
		}

		/// <summary>
		/// List all <see cref="CompileJob"/> <see cref="EntityId"/>s for the instance.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved <see cref="EntityId"/>s successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(DreamMakerRights.CompileJobs)]
		[ProducesResponseType(typeof(PaginatedResponse<CompileJobResponse>), 200)]
		public ValueTask<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> Paginated<CompileJob, CompileJobResponse>(
				() => ValueTask.FromResult(
					new PaginatableResult<CompileJob>(
						BaseCompileJobsQuery()
							.OrderByDescending(x => x.Job.StoppedAt))),
				null,
				page,
				pageSize,
				cancellationToken);

		/// <summary>
		/// Begin deploying repository code.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="202">Created deployment <see cref="JobResponse"/> successfully.</response>
		[HttpPost(Routes.Deploy)]
		[TgsAuthorize(DreamMakerRights.Compile)]
		[ProducesResponseType(typeof(JobResponse), 202)]
		public async ValueTask<IActionResult> Create(CancellationToken cancellationToken)
		{
			var job = Job.Create(JobCode.Deployment, AuthenticationContext.User, Instance, DreamMakerRights.CancelCompile);

			await jobManager.RegisterOperation(
				job,
				(core, databaseContextFactory, paramJob, progressReporter, jobCancellationToken)
					=> core!.DreamMaker.DeploymentProcess(paramJob, databaseContextFactory, progressReporter, jobCancellationToken),
				cancellationToken);
			return Accepted(job.ToApi());
		}

		/// <summary>
		/// Update deployment settings.
		/// </summary>
		/// <param name="model">The <see cref="DreamMakerRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Changes applied successfully. The updated <see cref="DreamMakerSettings"/> will be returned.</response>
		/// <response code="204">Changes applied successfully. The updated <see cref="DreamMakerSettings"/> will be not be returned due to permissions.</response>
		/// <response code="410">The database entity for the requested instance could not be retrieved. The instance was likely detached.</response>
		[HttpPost]
		[TgsAuthorize(
			DreamMakerRights.SetDme
			| DreamMakerRights.SetApiValidationPort
			| DreamMakerRights.SetSecurityLevel
			| DreamMakerRights.SetApiValidationRequirement
			| DreamMakerRights.SetTimeout
			| DreamMakerRights.SetCompilerArguments)]
		[ProducesResponseType(typeof(DreamMakerResponse), 200)]
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
#pragma warning disable CA1506 // TODO: Decomplexify
		public async ValueTask<IActionResult> Update([FromBody] DreamMakerRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);

			if (model.ApiValidationPort == 0)
				throw new InvalidOperationException("ApiValidationPort cannot be 0!");

			var hostModel = await DatabaseContext
				.DreamMakerSettings
				.AsQueryable()
				.Where(x => x.InstanceId == Instance.Id)
				.FirstOrDefaultAsync(cancellationToken);
			if (hostModel == null)
				return this.Gone();

			var dreamMakerRights = InstancePermissionSet.DreamMakerRights!.Value;
			if (model.ProjectName != null)
			{
				if (!dreamMakerRights.HasFlag(DreamMakerRights.SetDme))
					return Forbid();

				if (model.ProjectName.Length == 0) // can't use isnullorwhitespace because linux memes
					hostModel.ProjectName = null;
				else
					hostModel.ProjectName = model.ProjectName;
			}

			if (model.ApiValidationPort.HasValue)
			{
				if (!dreamMakerRights.HasFlag(DreamMakerRights.SetApiValidationPort))
					return Forbid();

				if (model.ApiValidationPort.Value != hostModel.ApiValidationPort!.Value)
				{
					Logger.LogTrace(
						"Triggering port allocator for DM-I:{instanceId} because model port {modelPort} doesn't match DB port {dbPort}...",
						Instance.Id,
						model.ApiValidationPort,
						hostModel.ApiValidationPort);
					var verifiedPort = await portAllocator
						.GetAvailablePort(
							model.ApiValidationPort.Value,
							true,
							cancellationToken);
					if (verifiedPort != model.ApiValidationPort)
						return Conflict(new ErrorMessageResponse(ErrorCode.PortNotAvailable));

					hostModel.ApiValidationPort = model.ApiValidationPort;
				}
			}

			if (model.ApiValidationSecurityLevel.HasValue)
			{
				if (!dreamMakerRights.HasFlag(DreamMakerRights.SetSecurityLevel))
					return Forbid();

				hostModel.ApiValidationSecurityLevel = model.ApiValidationSecurityLevel;
			}

#pragma warning disable CS0618 // Type or member is obsolete
			bool? legacyRequireDMApiValidation = model.RequireDMApiValidation;
#pragma warning restore CS0618 // Type or member is obsolete
			if (legacyRequireDMApiValidation.HasValue)
			{
				if (!dreamMakerRights.HasFlag(DreamMakerRights.SetApiValidationRequirement))
					return Forbid();

				hostModel.DMApiValidationMode = legacyRequireDMApiValidation.Value
					? DMApiValidationMode.Required
					: DMApiValidationMode.Optional;
			}

			if (model.DMApiValidationMode.HasValue)
			{
				if (legacyRequireDMApiValidation.HasValue)
					return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));

				if (!dreamMakerRights.HasFlag(DreamMakerRights.SetApiValidationRequirement))
					return Forbid();

				hostModel.DMApiValidationMode = model.DMApiValidationMode;
			}

			if (model.Timeout.HasValue)
			{
				if (!dreamMakerRights.HasFlag(DreamMakerRights.SetTimeout))
					return Forbid();

				hostModel.Timeout = model.Timeout;
			}

			if (model.CompilerAdditionalArguments != null)
			{
				if (!dreamMakerRights.HasFlag(DreamMakerRights.SetCompilerArguments))
					return Forbid();

				var sanitizedArguments = model.CompilerAdditionalArguments.Trim();
				if (sanitizedArguments.Length == 0)
					hostModel.CompilerAdditionalArguments = null;
				else
					hostModel.CompilerAdditionalArguments = sanitizedArguments;
			}

			await DatabaseContext.Save(cancellationToken);

			if (!dreamMakerRights.HasFlag(DreamMakerRights.Read))
				return NoContent();

			return await Read(cancellationToken);
		}
#pragma warning restore CA1506

		/// <summary>
		/// Base query for pulling in all required <see cref="CompileJob"/> fields.
		/// </summary>
		/// <returns>An <see cref="IQueryable{T}"/> of <see cref="CompileJob"/> with all the inclusions.</returns>
		IQueryable<CompileJob> BaseCompileJobsQuery() => DatabaseContext
			.CompileJobs
			.AsQueryable()
			.Include(x => x.Job!)
				.ThenInclude(x => x.StartedBy)
			.Include(x => x.Job!)
				.ThenInclude(x => x.Instance)
			.Include(x => x.RevisionInformation!)
				.ThenInclude(x => x.PrimaryTestMerge!)
					.ThenInclude(x => x.MergedBy)
			.Include(x => x.RevisionInformation)
				.ThenInclude(x => x.ActiveTestMerges!)
					.ThenInclude(x => x!.TestMerge)
						.ThenInclude(x => x!.MergedBy)
			.Where(x => x.Job.Instance!.Id == Instance.Id);
	}
}
