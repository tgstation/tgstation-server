using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	[Route("/" + nameof(DreamDaemon))]
	public sealed class DreamDaemonController : ModelController<Api.Models.DreamDaemon>
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
		/// Construct a <see cref="DreamDaemonController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		public DreamDaemonController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IJobManager jobManager, IInstanceManager instanceManager) : base(databaseContext, authenticationContextFactory)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamDaemonRights.Start)]
		public override async Task<IActionResult> Create([FromBody] Api.Models.DreamDaemon model, CancellationToken cancellationToken)
		{
			var instance = instanceManager.GetInstance(Instance);

			if (instance.DreamDaemon.Running)
				return StatusCode(HttpStatusCode.Gone);

			var launchParams = await DatabaseContext.Instances.Where(x => x.Id == Instance.Id).Select(x => new DreamDaemonLaunchParameters
			{
				AllowWebClient = x.DreamDaemonSettings.AllowWebClient,
				PrimaryPort = x.DreamDaemonSettings.PrimaryPort,
				SecondaryPort = x.DreamDaemonSettings.SecondaryPort,
				SecurityLevel = x.DreamDaemonSettings.SecurityLevel
			}).FirstAsync(cancellationToken).ConfigureAwait(false);

			await jobManager.RegisterOperation(new Models.Job
			{
				Description = "Launch DreamDaemon",
				CancelRight = (int)DreamDaemonRights.Shutdown,
				CancelRightsType = RightsType.DreamDaemon,
				Instance = Instance,
				StartedBy = AuthenticationContext.User
			}, (job, serviceProvider, innerCt) => instance.DreamDaemon.Launch(launchParams, innerCt), cancellationToken).ConfigureAwait(false);
			return Ok();
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamDaemonRights.ReadMetadata | DreamDaemonRights.ReadRevision)]
		public override async Task<IActionResult> Read(CancellationToken cancellationToken)
		{
			var dd = instanceManager.GetInstance(Instance).DreamDaemon;

			var metadata = (AuthenticationContext.GetRight(RightsType.DreamDaemon) & (int)DreamDaemonRights.ReadMetadata) != 0;
			var revision = (AuthenticationContext.GetRight(RightsType.DreamDaemon) & (int)DreamDaemonRights.ReadRevision) != 0;

			var settings = metadata ? await DatabaseContext.Instances.Where(x => x.Id == Instance.Id).Select(x => x.DreamDaemonSettings).FirstAsync(cancellationToken).ConfigureAwait(false) : null;
			Api.Models.DreamDaemon result = new Api.Models.DreamDaemon();
			if(metadata)
			{
				result.AutoStart = settings.AutoStart;
				result.CurrentPort = dd.CurrentPort;
				result.CurrentSecurity = dd.CurrentSecurity;
				result.PrimaryPort = dd.LastLaunchParameters.PrimaryPort;
				result.AllowWebClient = dd.LastLaunchParameters.AllowWebClient;
				result.Running = dd.Running;
				result.SecondaryPort = dd.LastLaunchParameters.SecondaryPort;
				result.SecurityLevel = dd.LastLaunchParameters.SecurityLevel;
				result.SoftRestart = dd.SoftRebooting;
				result.SoftShutdown = dd.SoftStopping;
			};
			if (revision)
				result.CompileJob = dd.LastCompileJob.ToApi();

			return Json(result);
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamDaemonRights.Shutdown)]
		public override async Task<IActionResult> Delete([FromBody] Api.Models.DreamDaemon model, CancellationToken cancellationToken)
		{
			var instance = instanceManager.GetInstance(Instance);

			if (!instance.DreamDaemon.Running)
				return StatusCode(HttpStatusCode.Gone);

			await instance.DreamDaemon.Terminate(false, cancellationToken).ConfigureAwait(false);
			return Ok();
		}
	
		[TgsAuthorize(DreamDaemonRights.SetAutoStart | DreamDaemonRights.SetPorts | DreamDaemonRights.SetSecurity | DreamDaemonRights.SetWebClient | DreamDaemonRights.SoftRestart | DreamDaemonRights.SoftShutdown)]
		public override async Task<IActionResult> Update([FromBody] Api.Models.DreamDaemon model, CancellationToken cancellationToken)
		{
			var current = await DatabaseContext.Instances.Where(x => x.Id == Instance.Id).Select(x => x.DreamDaemonSettings).FirstAsync(cancellationToken).ConfigureAwait(false);

			var userRights = (DreamDaemonRights)AuthenticationContext.GetRight(RightsType.DreamDaemon)
			if()
		}
	}
}
