using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
	[Route("/" + nameof(DreamMaker))]
    public sealed class DreamMakerController : ModelController<Api.Models.DreamMaker>
	{
		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="DreamMakerController"/>
		/// </summary>
		readonly IJobManager jobManager;
		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="DreamMakerController"/>
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// Construct a <see cref="DreamMakerController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/></param>
		public DreamMakerController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IJobManager jobManager, IInstanceManager instanceManager) : base(databaseContext, authenticationContextFactory, true)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamMakerRights.Read)]
		public override async Task<IActionResult> Read(CancellationToken cancellationToken)
		{
			var instance = instanceManager.GetInstance(Instance);
			var projectNameTask = DatabaseContext.DreamMakerSettings.Where(x => x.InstanceId == Instance.Id).Select(x => x.ProjectName).FirstAsync(cancellationToken);
			var job = await DatabaseContext.CompileJobs.OrderByDescending(x => x.Job.StartedAt).Include(x => x.Job).FirstAsync(cancellationToken).ConfigureAwait(false);
			return Json(new Api.Models.DreamMaker
			{
				LastJob = job.ToApi(),
				ProjectName = await projectNameTask.ConfigureAwait(false),
				Status = instance.DreamMaker.Status
			});
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamMakerRights.Compile)]
		public override async Task<IActionResult> Create([FromBody] Api.Models.DreamMaker model, CancellationToken cancellationToken)
		{
			var job = new Job
			{
				Description = "Compile active repository code",
				StartedBy = AuthenticationContext.User,
				CancelRightsType = RightsType.DreamMaker,
				CancelRight = (int)DreamMakerRights.CancelCompile,
				Instance = Instance
			};
			await jobManager.RegisterOperation(job, (paramJob, serviceProvider, ct) => RunCompile(paramJob, serviceProvider, Instance, ct), cancellationToken).ConfigureAwait(false);
			return Json(job);
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamMakerRights.CancelCompile)]
		public override async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
		{
			//alias for cancelling the latest job
			var job = await DatabaseContext.CompileJobs.OrderByDescending(x => x.Job.StartedAt).Select(x => new Job { Id = x.Job.Id, StoppedAt = x.Job.StoppedAt }).FirstAsync(cancellationToken).ConfigureAwait(false);
			if (job.StoppedAt != null)
				return StatusCode((int)HttpStatusCode.Gone);
			try
			{
				await jobManager.CancelJob(job, AuthenticationContext.User, cancellationToken).ConfigureAwait(false);
			}
			catch (InvalidOperationException)	//job already stopped
			{
				return StatusCode((int)HttpStatusCode.Gone);
			}
			return Ok();
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamMakerRights.SetDme)]
		public override async Task<IActionResult> Update([FromBody] Api.Models.DreamMaker model, CancellationToken cancellationToken)
		{
			var hostModel = new DreamMakerSettings
			{
				InstanceId = Instance.Id
			};
			DatabaseContext.DreamMakerSettings.Attach(hostModel);
			hostModel.ProjectName = model.ProjectName;
			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);
			return Ok();
		}

		/// <summary>
		/// Run the compile job and insert it into the database
		/// </summary>
		/// <param name="job">The running <see cref="Job"/></param>
		/// <param name="serviceProvider">The <see cref="IServiceProvider"/> for the operation</param>
		/// <param name="instanceModel">The <see cref="Models.Instance"/> for the operation</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		static async Task RunCompile(Job job, IServiceProvider serviceProvider, Models.Instance instanceModel, CancellationToken cancellationToken)
		{
			var instanceManager = serviceProvider.GetRequiredService<IInstanceManager>();
			var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();

			var timeoutTask = databaseContext.DreamDaemonSettings.Where(x => x.InstanceId == instanceModel.Id).Select(x => x.StartupTimeout).FirstAsync(cancellationToken);
			var projectName = await databaseContext.DreamMakerSettings.Where(x => x.InstanceId == instanceModel.Id).Select(x => x.ProjectName).FirstAsync(cancellationToken).ConfigureAwait(false);
			var timeout = await timeoutTask.ConfigureAwait(false);

			var instance = instanceManager.GetInstance(instanceModel);

			CompileJob compileJob;
			Task<RevisionInformation> revInfoTask;
			using (var repo = await instance.RepositoryManager.LoadRepository(cancellationToken).ConfigureAwait(false))
			{
				revInfoTask = databaseContext.RevisionInformations.Where(x => x.CommitSha == repo.Head).Select(x => new RevisionInformation { Id = x.Id }).FirstAsync();
				compileJob = await instance.DreamMaker.Compile(projectName, timeout.Value, repo, cancellationToken).ConfigureAwait(false);
			}

			compileJob.Job = job;
			compileJob.RevisionInformation = await revInfoTask.ConfigureAwait(false);

			databaseContext.CompileJobs.Add(compileJob);
			//default ct because we don't want to give up after getting this far
			await databaseContext.Save(default).ConfigureAwait(false);
		}
	}
}
