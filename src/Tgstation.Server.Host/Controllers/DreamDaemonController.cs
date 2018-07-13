using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ModelController{TModel}"/> for managing <see cref="Api.Models.DreamDaemon"/>
	/// </summary>
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
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/></param>
		public DreamDaemonController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IJobManager jobManager, IInstanceManager instanceManager) : base(databaseContext, authenticationContextFactory)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamDaemonRights.Start)]
		public override async Task<IActionResult> Create([FromBody] DreamDaemon model, CancellationToken cancellationToken)
		{
			var instance = instanceManager.GetInstance(Instance);

			if (instance.Watchdog.Running)
				return StatusCode((int)HttpStatusCode.Gone);

			await jobManager.RegisterOperation(new Models.Job
			{
				Description = "Launch DreamDaemon",
				CancelRight = (int)DreamDaemonRights.Shutdown,
				CancelRightsType = RightsType.DreamDaemon,
				Instance = Instance,
				StartedBy = AuthenticationContext.User
			}, 
			async (job, serviceProvider, innerCt) =>
			{
				var result = await instance.Watchdog.Launch(innerCt).ConfigureAwait(false);
				if (result == null)
					throw new InvalidOperationException("Watchdog already running!");
				if (!instance.Watchdog.Running)
					throw new Exception("Failed to launch watchdog!");
			},
			cancellationToken).ConfigureAwait(false);
			return Ok();
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamDaemonRights.ReadMetadata | DreamDaemonRights.ReadRevision)]
		public override async Task<IActionResult> Read(CancellationToken cancellationToken)
		{
			var dd = instanceManager.GetInstance(Instance).Watchdog;

			var metadata = (AuthenticationContext.GetRight(RightsType.DreamDaemon) & (int)DreamDaemonRights.ReadMetadata) != 0;
			var revision = (AuthenticationContext.GetRight(RightsType.DreamDaemon) & (int)DreamDaemonRights.ReadRevision) != 0;

			var settings = await DatabaseContext.Instances.Where(x => x.Id == Instance.Id).Select(x => x.DreamDaemonSettings).FirstAsync(cancellationToken).ConfigureAwait(false);
			var result = new DreamDaemon();
			if(metadata)
			{
				var alphaActive = dd.AlphaIsActive;
				var llp = dd.LastLaunchParameters;
				result.AutoStart = settings.AutoStart;
				result.CurrentPort = alphaActive ? llp.PrimaryPort : llp.SecondaryPort;
				result.CurrentSecurity = llp.SecurityLevel;
				result.CurrentAllowWebclient = llp.AllowWebClient;
				result.PrimaryPort = dd.ActiveLaunchParameters.PrimaryPort;
				result.AllowWebClient = dd.ActiveLaunchParameters.AllowWebClient;
				result.Running = dd.Running;
				result.SecondaryPort = dd.LastLaunchParameters.SecondaryPort;
				result.SecurityLevel = dd.LastLaunchParameters.SecurityLevel;
				var rstate = dd.RebootState;
				result.SoftRestart = rstate == RebootState.Restart;
				result.SoftShutdown = rstate == RebootState.Shutdown;
			};
			if (revision)
			{
				result.ActiveCompileJob = settings.ActiveCompileJob?.ToApi();
				result.StagedCompileJob = settings.StagedCompileJob?.ToApi();
			}

			return Json(result);
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamDaemonRights.Shutdown)]
		public override async Task<IActionResult> Delete([FromBody] DreamDaemon model, CancellationToken cancellationToken)
		{
			var instance = instanceManager.GetInstance(Instance);

			if (!instance.Watchdog.Running)
				return StatusCode((int)HttpStatusCode.Gone);

			await instance.Watchdog.Terminate(false, cancellationToken).ConfigureAwait(false);
			return Ok();
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamDaemonRights.SetAutoStart | DreamDaemonRights.SetPorts | DreamDaemonRights.SetSecurity | DreamDaemonRights.SetWebClient | DreamDaemonRights.SoftRestart | DreamDaemonRights.SoftShutdown | DreamDaemonRights.Start | DreamDaemonRights.SetStartupTimeout)]
		public override async Task<IActionResult> Update([FromBody] DreamDaemon model, CancellationToken cancellationToken)
		{
			var current = await DatabaseContext.Instances.Where(x => x.Id == Instance.Id).Select(x => x.DreamDaemonSettings).FirstAsync(cancellationToken).ConfigureAwait(false);

			var userRights = (DreamDaemonRights)AuthenticationContext.GetRight(RightsType.DreamDaemon);

			bool CheckModified<T>(Expression<Func<Api.Models.Internal.DreamDaemonSettings, T>> expression, DreamDaemonRights requiredRight)
			{
				var memberSelectorExpression = (MemberExpression)expression.Body;
				var property = (PropertyInfo)memberSelectorExpression.Member;

				var newVal = property.GetValue(model);
				if (newVal == null)
					return false;
				if (!userRights.HasFlag(requiredRight) && property.GetValue(current) != newVal)
					return true;

				property.SetValue(current, newVal);
				return false;
			};

			var oldSoftRestart = current.SoftRestart;
			var oldSoftShutdown = current.SoftShutdown;

			if (!CheckModified(x => x.AllowWebClient, DreamDaemonRights.SetWebClient)
				|| !CheckModified(x => x.AutoStart, DreamDaemonRights.SetAutoStart)
				|| !CheckModified(x => x.PrimaryPort, DreamDaemonRights.SetPorts)
				|| !CheckModified(x => x.SecondaryPort, DreamDaemonRights.SetPorts)
				|| !CheckModified(x => x.SecurityLevel, DreamDaemonRights.SetSecurity)
				|| !CheckModified(x => x.SoftRestart, DreamDaemonRights.SoftRestart)
				|| !CheckModified(x => x.SoftShutdown, DreamDaemonRights.SoftShutdown)
				|| !CheckModified(x => x.StartupTimeout, DreamDaemonRights.SetStartupTimeout))
				return Forbid();
			
			var wd = instanceManager.GetInstance(Instance).Watchdog;
			
			var changeSettingsTask = wd.ChangeSettings(current, cancellationToken);

			//soft shutdown/restart can't be cancelled because of how many things rely on them
			//They can be alternated though
			if (!oldSoftRestart.Value && current.SoftRestart.Value)
				await Task.WhenAll(changeSettingsTask, wd.Restart(true, cancellationToken)).ConfigureAwait(false);
			else if (!oldSoftShutdown.Value && current.SoftShutdown.Value)
				await Task.WhenAll(changeSettingsTask, wd.Terminate(true, cancellationToken)).ConfigureAwait(false);
			else
				await changeSettingsTask.ConfigureAwait(false);

			await DatabaseContext.Save(default).ConfigureAwait(false);

			return Ok();
		}
	}
}
