using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Controller for managing the compiler
	/// </summary>
	[Route("/DreamMaker")]
    public sealed class DreamMakerController : ModelController<Api.Models.CompileJob>
	{
		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="DreamMakerController"/>
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// Construct a <see cref="HomeController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		public DreamMakerController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IJobManager jobManager) : base(databaseContext, authenticationContextFactory) => this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));

		/// <inheritdoc />
		[TgsAuthorize(DreamMakerRights.Compile)]
		public override async Task<IActionResult> Create([FromBody] Api.Models.CompileJob model, CancellationToken cancellationToken)
		{
			var job = new Job
			{
				Description = "Compile active repository code",
				StartedBy = AuthenticationContext.User
			};
			await jobManager.RegisterOperation(job, (serviceProvider, ct) => RunCompile(serviceProvider, Instance, AuthenticationContext.Clone(), ct), cancellationToken).ConfigureAwait(false);
			return Json(job);
		}

		/// <inheritdoc />
		public override async Task<IActionResult> Delete([FromBody] Api.Models.CompileJob model, CancellationToken cancellationToken)
		{
			//alias for cancelling the latest job
			var job = await DatabaseContext.Jobs.OrderByDescending(x => x.StartedAt).Select(x => new Job { Id = x.Id, StoppedAt = x.StoppedAt }).FirstAsync(cancellationToken).ConfigureAwait(false);
			if (job.StoppedAt != null)
				return StatusCode(HttpStatusCode.Gone);
			jobManager.CancelJob(job);
			return Ok();
		}

		/// <summary>
		/// Run the compile job and insert it into the database
		/// </summary>
		/// <param name="serviceProvider">The <see cref="IServiceProvider"/> for the operation</param>
		/// <param name="instanceModel">The <see cref="Models.Instance"/> for the operation</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the operation</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns></returns>
		static async Task RunCompile(IServiceProvider serviceProvider, Models.Instance instanceModel, IAuthenticationContext authenticationContext, CancellationToken cancellationToken)
		{
			var instanceManager = serviceProvider.GetRequiredService<IInstanceManager>();
			var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();

			var projectName = await databaseContext.Instances.Where(x => x.Id == instanceModel.Id).Select(x => x.DreamMakerSettings.ProjectName).FirstAsync(cancellationToken).ConfigureAwait(false);

			var instance = instanceManager.GetInstance(instanceModel);

			CompileJob compileJob;
			Task<RevisionInformation> revInfoTask;
			using (var repo = await instance.RepositoryManager.LoadRepository(cancellationToken).ConfigureAwait(false))
			{
				revInfoTask = databaseContext.RevisionInformations.Where(x => x.Commit == repo.Head).Select(x => new RevisionInformation { Id = x.Id }).FirstAsync();
				compileJob = await instance.DreamMaker.Compile(projectName, repo, cancellationToken).ConfigureAwait(false);
			}

			compileJob.TriggeredBy = authenticationContext.User;
			compileJob.RevisionInformation = await revInfoTask.ConfigureAwait(false);

			databaseContext.CompileJobs.Add(compileJob);
			//default ct because we don't want to give up after getting this far
			await databaseContext.Save(default).ConfigureAwait(false);
		}
	}
}
