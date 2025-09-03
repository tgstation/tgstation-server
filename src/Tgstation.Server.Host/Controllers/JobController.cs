using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
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
	/// <see cref="ApiController"/> for <see cref="Job"/>s.
	/// </summary>
	[Route(Routes.Jobs)]
	public sealed class JobController : InstanceRequiredController
	{
		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="JobController"/>.
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// Initializes a new instance of the <see cref="JobController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="apiHeaders">The <see cref="IApiHeadersProvider"/> for the <see cref="InstanceRequiredController"/>.</param>
		public JobController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			ILogger<JobController> logger,
			IInstanceManager instanceManager,
			IJobManager jobManager,
			IApiHeadersProvider apiHeaders)
			: base(
				  databaseContext,
				  authenticationContext,
				  logger,
				  instanceManager,
				  apiHeaders)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
		}

		/// <summary>
		/// Get active <see cref="JobResponse"/>s for the instance.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved active <see cref="Job"/>s successfully.</response>
		[HttpGet]
		[TgsAuthorize]
		[ProducesResponseType(typeof(PaginatedResponse<JobResponse>), 200)]
		public ValueTask<IActionResult> Read([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> Paginated<Job, JobResponse>(
				() => ValueTask.FromResult<PaginatableResult<Job>?>(
					new PaginatableResult<Job>(
						DatabaseContext
						.Jobs
						.AsQueryable()
						.Include(x => x.StartedBy)
						.Include(x => x.CancelledBy)
						.Include(x => x.Instance)
						.Where(x => x.Instance!.Id == Instance.Id && !x.StoppedAt.HasValue)
						.OrderByDescending(x => x.StartedAt))),
				AddJobProgressResponseTransformer,
				page,
				pageSize,
				cancellationToken);

		/// <summary>
		/// List all <see cref="JobResponse"/> for the instance in reverse creation order.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved <see cref="Job"/> <see cref="EntityId"/>s successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize]
		[ProducesResponseType(typeof(PaginatedResponse<JobResponse>), 200)]
		public ValueTask<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> Paginated<Job, JobResponse>(
				() => ValueTask.FromResult<PaginatableResult<Job>?>(
					new PaginatableResult<Job>(
						DatabaseContext
						.Jobs
						.AsQueryable()
						.Include(x => x.StartedBy)
						.Include(x => x.CancelledBy)
						.Include(x => x.Instance)
						.Where(x => x.Instance!.Id == Instance.Id)
						.OrderByDescending(x => x.StartedAt))),
				AddJobProgressResponseTransformer,
				page,
				pageSize,
				cancellationToken);

		/// <summary>
		/// Cancel a running <see cref="JobResponse"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/> of the <see cref="JobResponse"/> to cancel.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="202"><see cref="Job"/> cancellation requested successfully.</response>
		/// <response code="404"><see cref="Job"/> does not exist in this instance.</response>
		/// <response code="410"><see cref="Job"/> could not be found in the job manager. Has it already completed?.</response>
		[HttpDelete("{id}")]
		[HttpPost("{id}")]
		[TgsAuthorize]
		[ProducesResponseType(typeof(JobResponse), 202)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 404)]
		public async ValueTask<IActionResult> Delete(long id, CancellationToken cancellationToken)
		{
			// don't care if an instance post or not at this point
			var job = await DatabaseContext
				.Jobs
				.AsQueryable()
				.Include(x => x.StartedBy)
				.Include(x => x.Instance)
				.Where(x => x.Id == id && x.Instance!.Id == Instance.Id)
				.FirstOrDefaultAsync(cancellationToken);
			if (job == default)
				return NotFound();

			if (job.StoppedAt != null)
				return Conflict(new ErrorMessageResponse(ErrorCode.JobStopped));

			if (job.CancelRight.HasValue && job.CancelRightsType.HasValue && (AuthenticationContext.GetRight(job.CancelRightsType.Value) & job.CancelRight.Value) == 0)
				return Forbid();

			var updatedJob = await jobManager.CancelJob(job, AuthenticationContext.User, false, cancellationToken);
			return updatedJob != null ? Accepted(updatedJob.ToApi()) : this.Gone();
		}

		/// <summary>
		/// Get a specific <see cref="JobResponse"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/> of the <see cref="JobResponse"/> to retrieve.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved <see cref="Job"/> successfully.</response>
		/// <response code="404"><see cref="Job"/> does not exist in this instance.</response>
		[HttpGet("{id}")]
		[TgsAuthorize]
		[ProducesResponseType(typeof(JobResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 404)]
		public async ValueTask<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			var job = await DatabaseContext
				.Jobs
				.AsQueryable()
				.Where(x => x.Id == id && x.Instance!.Id == Instance.Id)
				.Include(x => x.StartedBy)
				.Include(x => x.CancelledBy)
				.Include(x => x.Instance)
				.FirstOrDefaultAsync(cancellationToken);
			if (job == default)
				return NotFound();
			var api = job.ToApi();
			jobManager.SetJobProgress(api);
			return Json(api);
		}

		/// <summary>
		/// Supplements <see cref="JobResponse"/> <see cref="PaginatedResponse{TModel}"/>s with their <see cref="JobResponse.Progress"/>.
		/// </summary>
		/// <param name="jobResponse">The <see cref="JobResponse"/> to augment.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask AddJobProgressResponseTransformer(JobResponse jobResponse)
		{
			jobManager.SetJobProgress(jobResponse);
			return ValueTask.CompletedTask;
		}
	}
}
