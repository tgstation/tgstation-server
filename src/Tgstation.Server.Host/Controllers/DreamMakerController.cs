using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing the deployment system.
	/// </summary>
	[Route(Routes.DreamMaker)]
	#pragma warning disable CA1506 // TODO: Decomplexify
	public sealed class DreamMakerController : InstanceRequiredController
	{
		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="DreamMakerController"/>
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="IPortAllocator"/> for the <see cref="DreamMakerController"/>.
		/// </summary>
		readonly IPortAllocator portAllocator;

		/// <summary>
		/// Construct a <see cref="DreamMakerController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="portAllocator">The value of <see cref="IPortAllocator"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		public DreamMakerController(
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			IJobManager jobManager,
			IInstanceManager instanceManager,
			IPortAllocator portAllocator,
			ILogger<DreamMakerController> logger)
			: base(
				  instanceManager,
				  databaseContext,
				  authenticationContextFactory,
				  logger)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.portAllocator = portAllocator ?? throw new ArgumentNullException(nameof(portAllocator));
		}

		/// <summary>
		/// Read current <see cref="DreamMaker"/> status.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Read <see cref="DreamMaker"/> status successfully.</response>
		[HttpGet]
		[TgsAuthorize(DreamMakerRights.Read)]
		[ProducesResponseType(typeof(DreamMaker), 200)]
		public async Task<IActionResult> Read(CancellationToken cancellationToken)
		{
			var dreamMakerSettings = await DatabaseContext
				.DreamMakerSettings
				.AsQueryable()
				.Where(x => x.InstanceId == Instance.Id)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);
			return Json(dreamMakerSettings.ToApi());
		}

		/// <summary>
		/// Get a <see cref="Api.Models.CompileJob"/> specified by a given <paramref name="id"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200"><see cref="Api.Models.CompileJob"/> retrieved successfully.</response>
		/// <response code="404">Specified <see cref="Api.Models.CompileJob"/> ID does not exist in this instance.</response>
		[HttpGet("{id}")]
		[TgsAuthorize(DreamMakerRights.CompileJobs)]
		[ProducesResponseType(typeof(Api.Models.CompileJob), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 404)]
		public async Task<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			var compileJob = await BaseCompileJobsQuery()
				.Where(x => x.Id == id)
				.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (compileJob == default)
				return NotFound();
			return Json(compileJob.ToApi());
		}

		/// <summary>
		/// Base query for pulling in all required <see cref="CompileJob"/> fields.
		/// </summary>
		/// <returns>An <see cref="IQueryable{T}"/> of <see cref="CompileJob"/> with all the inclusions.</returns>
		IQueryable<Models.CompileJob> BaseCompileJobsQuery() => DatabaseContext
			.CompileJobs
			.AsQueryable()
			.Include(x => x.Job)
				.ThenInclude(x => x.StartedBy)
			.Include(x => x.RevisionInformation)
				.ThenInclude(x => x.PrimaryTestMerge)
					.ThenInclude(x => x.MergedBy)
			.Include(x => x.RevisionInformation)
				.ThenInclude(x => x.ActiveTestMerges)
					.ThenInclude(x => x.TestMerge)
						.ThenInclude(x => x.MergedBy)
			.Where(x => x.Job.Instance.Id == Instance.Id);

		/// <summary>
		/// List all <see cref="Api.Models.CompileJob"/> <see cref="EntityId"/>s for the instance.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved <see cref="EntityId"/>s successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(DreamMakerRights.CompileJobs)]
		[ProducesResponseType(typeof(Paginated<Api.Models.CompileJob>), 200)]
		public Task<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> Paginated<Models.CompileJob, Api.Models.CompileJob>(
				() => Task.FromResult(
					new PaginatableResult<Models.CompileJob>(
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
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="202">Created deployment <see cref="Api.Models.Job"/> successfully.</response>
		[HttpPut]
		[TgsAuthorize(DreamMakerRights.Compile)]
		[ProducesResponseType(typeof(Api.Models.Job), 202)]
		public async Task<IActionResult> Create(CancellationToken cancellationToken)
		{
			var job = new Models.Job
			{
				Description = "Compile active repository code",
				StartedBy = AuthenticationContext.User,
				CancelRightsType = RightsType.DreamMaker,
				CancelRight = (ulong)DreamMakerRights.CancelCompile,
				Instance = Instance
			};

			await jobManager.RegisterOperation(
				job,
				(core, databaseContextFactory, paramJob, progressReporter, jobCancellationToken)
					=> core.DreamMaker.DeploymentProcess(paramJob, databaseContextFactory, progressReporter, jobCancellationToken),
				cancellationToken)
				.ConfigureAwait(false);
			return Accepted(job.ToApi());
		}

		/// <summary>
		/// Update deployment settings.
		/// </summary>
		/// <param name="model">The updated <see cref="DreamMaker"/> settings.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Changes applied successfully. The updated <see cref="DreamMaker"/> settings will be returned.</response>
		/// <response code="204">Changes applied successfully. The updated <see cref="DreamMaker"/> settings will be not be returned due to permissions.</response>
		/// <response code="410">The database entity for the requested instance could not be retrieved. The instance was likely detached.</response>
		[HttpPost]
		[TgsAuthorize(
			DreamMakerRights.SetDme
			| DreamMakerRights.SetApiValidationPort
			| DreamMakerRights.SetSecurityLevel
			| DreamMakerRights.SetApiValidationRequirement)]
		[ProducesResponseType(typeof(DreamMaker), 200)]
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(ErrorMessage), 410)]
		public async Task<IActionResult> Update([FromBody] DreamMaker model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.ApiValidationPort == 0)
				throw new InvalidOperationException("ApiValidationPort cannot be 0!");

			var hostModel = await DatabaseContext
				.DreamMakerSettings
				.AsQueryable()
				.Where(x => x.InstanceId == Instance.Id)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);
			if (hostModel == null)
				return Gone();

			if (model.ProjectName != null)
			{
				if (!AuthenticationContext.InstancePermissionSet.DreamMakerRights.Value.HasFlag(DreamMakerRights.SetDme))
					return Forbid();
				if (model.ProjectName.Length == 0)
					hostModel.ProjectName = null;
				else
					hostModel.ProjectName = model.ProjectName;
			}

			if (model.ApiValidationPort.HasValue)
			{
				if (!AuthenticationContext.InstancePermissionSet.DreamMakerRights.Value.HasFlag(DreamMakerRights.SetApiValidationPort))
					return Forbid();

				if (model.ApiValidationPort.Value != hostModel.ApiValidationPort.Value)
				{
					var verifiedPort = await portAllocator
						.GetAvailablePort(
							model.ApiValidationPort.Value,
							true,
							cancellationToken)
							.ConfigureAwait(false);
					if (verifiedPort != model.ApiValidationPort)
						return Conflict(new ErrorMessage(ErrorCode.PortNotAvailable));

					hostModel.ApiValidationPort = model.ApiValidationPort;
				}
			}

			if (model.ApiValidationSecurityLevel.HasValue)
			{
				if (!AuthenticationContext.InstancePermissionSet.DreamMakerRights.Value.HasFlag(DreamMakerRights.SetSecurityLevel))
					return Forbid();
				hostModel.ApiValidationSecurityLevel = model.ApiValidationSecurityLevel;
			}

			if (model.RequireDMApiValidation.HasValue)
			{
				if (!AuthenticationContext.InstancePermissionSet.DreamMakerRights.Value.HasFlag(DreamMakerRights.SetApiValidationRequirement))
					return Forbid();
				hostModel.RequireDMApiValidation = model.RequireDMApiValidation;
			}

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			if ((AuthenticationContext.GetRight(RightsType.DreamMaker) & (ulong)DreamMakerRights.Read) == 0)
				return NoContent();

			return await Read(cancellationToken).ConfigureAwait(false);
		}
	}
}
