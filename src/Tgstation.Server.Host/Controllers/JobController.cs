using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ModelController{TModel}"/> for <see cref="Api.Models.Job"/>s
	/// </summary>
	[Route(Routes.Jobs)]
	public sealed class JobController : ModelController<Api.Models.Job>
	{
		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="JobController"/>
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// Construct a <see cref="HomeController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		public JobController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IJobManager jobManager, ILogger<JobController> logger) : base(databaseContext, authenticationContextFactory, logger, true)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
		}

		/// <inheritdoc />
		[TgsAuthorize]
		public override async Task<IActionResult> Read(CancellationToken cancellationToken)
		{
			var result = await DatabaseContext.Jobs.Where(x => x.Instance.Id == Instance.Id && !x.StoppedAt.HasValue).OrderByDescending(x => x.StartedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
			return Json(result.Select(x => x.ToApi()));
		}

		/// <inheritdoc />
		[TgsAuthorize]
		public override async Task<IActionResult> List(CancellationToken cancellationToken)
		{
			// you KNOW this will need pagination eventually right?
			var jobs = await DatabaseContext.Jobs.Where(x => x.Instance.Id == Instance.Id).OrderByDescending(x => x.StartedAt).Select(x => new Api.Models.Job
			{
				Id = x.Id
			}).ToListAsync(cancellationToken).ConfigureAwait(false);
			return Json(jobs);
		}

		/// <inheritdoc />
		[TgsAuthorize]
		public override async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
		{
			// don't care if an instance post or not at this point
			var job = await DatabaseContext.Jobs.Where(x => x.Id == id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (job == default(Job))
				return NotFound();

			if (job.StoppedAt != null)
				return StatusCode((int)HttpStatusCode.Gone);

			if (job.CancelRight.HasValue && job.CancelRightsType.HasValue && (AuthenticationContext.GetRight(job.CancelRightsType.Value) & job.CancelRight.Value) == 0)
				return Forbid();

			var cancelled = await jobManager.CancelJob(job, AuthenticationContext.User, false, cancellationToken).ConfigureAwait(false);
			return cancelled ? (IActionResult)Accepted() : StatusCode((int)HttpStatusCode.Gone);
		}

		/// <inheritdoc />
		[TgsAuthorize]
		public override async Task<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			var job = await DatabaseContext.Jobs.Where(x => x.Id == id).Include(x => x.StartedBy).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (job == default(Job))
				return NotFound();
			var api = job.ToApi();
			api.Progress = jobManager.JobProgress(job);
			return Json(api);
		}
	}
}
