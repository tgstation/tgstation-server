using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
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
	[Route(Routes.DreamMaker)]
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
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		public DreamMakerController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IJobManager jobManager, IInstanceManager instanceManager, ILogger<DreamMakerController> logger) : base(databaseContext, authenticationContextFactory, logger, true)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamMakerRights.Read)]
		public override async Task<IActionResult> Read(CancellationToken cancellationToken)
		{
			var instance = instanceManager.GetInstance(Instance);
			var dreamMakerSettings = await DatabaseContext.DreamMakerSettings.Where(x => x.InstanceId == Instance.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			return Json(new DreamMaker
			{
				ProjectName = dreamMakerSettings.ProjectName,
				ApiValidationPort = dreamMakerSettings.ApiValidationPort
			});
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamMakerRights.CompileJobs)]
		public override async Task<IActionResult> GetId(long id, CancellationToken cancellationToken)
		{
			var compileJob = await DatabaseContext.CompileJobs
				.Where(x => x.Id == id && x.Job.Instance.Id == Instance.Id)
				.Include(x => x.Job).ThenInclude(x => x.StartedBy)
				.Include(x => x.RevisionInformation).ThenInclude(x => x.PrimaryTestMerge)
				.Include(x => x.RevisionInformation).ThenInclude(x => x.ActiveTestMerges).ThenInclude(x => x.TestMerge)
				.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (compileJob == default)
				return NotFound();
			return Json(compileJob.ToApi());
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamMakerRights.CompileJobs)]
		public override async Task<IActionResult> List(CancellationToken cancellationToken)
		{
			var compileJobs = await DatabaseContext.CompileJobs.Where(x => x.Job.Instance.Id == Instance.Id).OrderByDescending(x => x.Job.StoppedAt).Select(x => new Api.Models.CompileJob
			{
				Id = x.Id
			}).ToListAsync(cancellationToken).ConfigureAwait(false);
			return Json(compileJobs);
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamMakerRights.Compile)]
		public override async Task<IActionResult> Create([FromBody] DreamMaker model, CancellationToken cancellationToken)
		{
			var job = new Models.Job
			{
				Description = "Compile active repository code",
				StartedBy = AuthenticationContext.User,
				CancelRightsType = RightsType.DreamMaker,
				CancelRight = (ulong)DreamMakerRights.CancelCompile,
				Instance = Instance
			};
			await jobManager.RegisterOperation(job, instanceManager.GetInstance(Instance).CompileProcess, cancellationToken).ConfigureAwait(false);
			return Accepted(job.ToApi());
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamMakerRights.SetDme | DreamMakerRights.SetApiValidationPort | DreamMakerRights.SetApiValidationPort)]
		public override async Task<IActionResult> Update([FromBody] DreamMaker model, CancellationToken cancellationToken)
		{
			if (model.ApiValidationPort == 0)
				return BadRequest(new ErrorMessage { Message = "API Validation port cannot be 0!" });

			if (model.ApiValidationSecurityLevel == DreamDaemonSecurity.Ultrasafe)
				return BadRequest(new ErrorMessage { Message = "This version of TGS does not support the ultrasafe DreamDaemon configuration!" });

			var hostModel = await DatabaseContext.DreamMakerSettings.Where(x => x.InstanceId == Instance.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (hostModel == null)
				return StatusCode((int)HttpStatusCode.Gone);

			if (model.ProjectName != null)
			{
				if (!AuthenticationContext.InstanceUser.DreamMakerRights.Value.HasFlag(DreamMakerRights.SetDme))
					return Forbid();
				if (model.ProjectName.Length == 0)
					hostModel.ProjectName = null;
				else
					hostModel.ProjectName = model.ProjectName;
			}

			if (model.ApiValidationPort.HasValue)
			{
				if (!AuthenticationContext.InstanceUser.DreamMakerRights.Value.HasFlag(DreamMakerRights.SetApiValidationPort))
					return Forbid();
				hostModel.ApiValidationPort = model.ApiValidationPort;
			}

			if (model.ApiValidationSecurityLevel.HasValue)
			{
				if (!AuthenticationContext.InstanceUser.DreamMakerRights.Value.HasFlag(DreamMakerRights.SetSecurityLevel))
					return Forbid();
				hostModel.ApiValidationSecurityLevel = model.ApiValidationSecurityLevel;
			}

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);
			return await Read(cancellationToken).ConfigureAwait(false);
		}
	}
}
