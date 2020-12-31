using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for <see cref="Api.Models.Job"/>s
	/// </summary>
	[Route(Routes.Jobs)]
	public sealed class JobController : InstanceRequiredController
	{
		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="JobController"/>
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// Construct a <see cref="HomeController"/>
		/// </summary>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		public JobController(
			IInstanceManager instanceManager,
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			IJobManager jobManager,
			ILogger<JobController> logger)
			: base(
				  instanceManager,
				  databaseContext,
				  authenticationContextFactory,
				  logger)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
		}

		/// <summary>
		/// Get active <see cref="Api.Models.Job"/>s for the instance.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved active <see cref="Api.Models.Job"/>s successfully.</response>
		[HttpGet]
		[TgsAuthorize]
		[ProducesResponseType(typeof(Paginated<Api.Models.Job>), 200)]
		public Task<IActionResult> Read([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> Paginated<Models.Job, Api.Models.Job>(
				() => Task.FromResult(
					new PaginatableResult<Models.Job>(
						DatabaseContext
						.Jobs
						.AsQueryable()
						.Include(x => x.StartedBy)
						.Where(x => x.Instance.Id == Instance.Id && !x.StoppedAt.HasValue)
						.OrderByDescending(x => x.StartedAt))),
				null,
				page,
				pageSize,
				cancellationToken);

		/// <summary>
		/// List all <see cref="Api.Models.Job"/> <see cref="EntityId"/>s for the instance in reverse creation order.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved <see cref="Api.Models.Job"/> <see cref="EntityId"/>s successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize]
		[ProducesResponseType(typeof(Paginated<Api.Models.Job>), 200)]
		public Task<IActionResult> List([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> Paginated<Models.Job, Api.Models.Job>(
				() => Task.FromResult(
					new PaginatableResult<Models.Job>(
						DatabaseContext
						.Jobs
						.AsQueryable()
						.Include(x => x.StartedBy)
						.Where(x => x.Instance.Id == Instance.Id)
						.OrderByDescending(x => x.StartedAt))),
				null,
				page,
				pageSize,
				cancellationToken);

		/// <summary>
		/// Cancel a running <see cref="Api.Models.Job"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/> of the <see cref="Api.Models.Job"/> to cancel.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="202"><see cref="Api.Models.Job"/> cancellation requested successfully.</response>
		/// <response code="404"><see cref="Api.Models.Job"/> does not exist in this instance.</response>
		/// <response code="410"><see cref="Api.Models.Job"/> could not be found in the job manager. Has it already completed?</response>
		[HttpDelete("{id}")]
		[TgsAuthorize]
		[ProducesResponseType(typeof(Api.Models.Job), 202)]
		[ProducesResponseType(typeof(ErrorMessage), 404)]
		public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
		{
			// don't care if an instance post or not at this point
			var job = await DatabaseContext
				.Jobs
				.AsQueryable()
				.Where(x => x.Id == id && x.Instance.Id == Instance.Id)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);
			if (job == default)
				return NotFound();

			if (job.StoppedAt != null)
				return Conflict(new ErrorMessage(ErrorCode.JobStopped));

			if (job.CancelRight.HasValue && job.CancelRightsType.HasValue && (AuthenticationContext.GetRight(job.CancelRightsType.Value) & job.CancelRight.Value) == 0)
				return Forbid();

			var updatedJob = await jobManager.CancelJob(job, AuthenticationContext.User, false, cancellationToken).ConfigureAwait(false);
			return updatedJob != null ? Accepted(updatedJob.ToApi()) : Gone();
		}

		/// <summary>
		/// Get a specific <see cref="Api.Models.Job"/>.
		/// </summary>
		/// <param name="id">The <see cref="EntityId.Id"/> of the <see cref="Api.Models.Job"/> to retrieve.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Retrieved <see cref="Api.Models.Job"/> successfully.</response>
		/// <response code="404"><see cref="Api.Models.Job"/> does not exist in this instance.</response>
		[HttpGet("{id}")]
		[TgsAuthorize]
		[ProducesResponseType(typeof(Api.Models.Job), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 404)]
		public async Task<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			var job = await DatabaseContext
				.Jobs
				.AsQueryable()
				.Where(x => x.Id == id && x.Instance.Id == Instance.Id)
				.Include(x => x.StartedBy)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);
			if (job == default)
				return NotFound();
			var api = job.ToApi();
			api.Progress = jobManager.JobProgress(job);
			return Json(api);
		}
	}
}
