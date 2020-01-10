using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
	/// <see cref="ApiController"/> for <see cref="Api.Models.Job"/>s
	/// </summary>
	[Route(Routes.Jobs)]
	public sealed class JobController : ApiController
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
		public JobController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IJobManager jobManager, ILogger<JobController> logger) : base(databaseContext, authenticationContextFactory, logger, true, true)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
		}

		[HttpGet]
		[TgsAuthorize]
		[ProducesResponseType(typeof(IEnumerable<Api.Models.Job>), 200)]
		public async Task<IActionResult> Read(CancellationToken cancellationToken)
		{
			var result = await DatabaseContext.Jobs.Where(x => x.Instance.Id == Instance.Id && !x.StoppedAt.HasValue).OrderByDescending(x => x.StartedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
			return Json(result.Select(x => x.ToApi()));
		}

		[HttpGet(Routes.List)]
		[TgsAuthorize]
		[ProducesResponseType(typeof(List<Api.Models.Job>), 200)]
		public async Task<IActionResult> List(CancellationToken cancellationToken)
		{
			// you KNOW this will need pagination eventually right?
			var jobs = await DatabaseContext.Jobs.Where(x => x.Instance.Id == Instance.Id).OrderByDescending(x => x.StartedAt).Select(x => new Api.Models.Job
			{
				Id = x.Id
			}).ToListAsync(cancellationToken).ConfigureAwait(false);
			return Json(jobs);
		}

		[HttpDelete]
		[TgsAuthorize]
		[ProducesResponseType(202)]
		[ProducesResponseType(404)]
		[ProducesResponseType(410)]
		public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
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

		[HttpGet("{id}")]
		[TgsAuthorize]
		[ProducesResponseType(404)]
		[ProducesResponseType(typeof(Api.Models.Job), 200)]
		public async Task<IActionResult> GetId(long id, CancellationToken cancellationToken)
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
