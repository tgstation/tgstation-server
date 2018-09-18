using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
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
	/// <see cref="ModelController{TModel}"/> for managing the <see cref="DreamDaemon"/>
	/// </summary>
	[Route(Routes.DreamDaemon)]
	public sealed class DreamDaemonController : ModelController<DreamDaemon>
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
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		public DreamDaemonController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IJobManager jobManager, IInstanceManager instanceManager, ILogger<DreamDaemonController> logger) : base(databaseContext, authenticationContextFactory, logger, true)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamDaemonRights.Start)]
		public override async Task<IActionResult> Create([FromBody] DreamDaemon model, CancellationToken cancellationToken)
		{
			//alias for launching DD
			var instance = instanceManager.GetInstance(Instance);

			if (instance.Watchdog.Running)
				return StatusCode((int)HttpStatusCode.Gone);

			var job = new Models.Job
			{
				Description = "Launch DreamDaemon",
				CancelRight = (ulong)DreamDaemonRights.Shutdown,
				CancelRightsType = RightsType.DreamDaemon,
				Instance = Instance,
				StartedBy = AuthenticationContext.User
			};
			await jobManager.RegisterOperation(job,
			async (paramJob, databaseContext, progressHandler, innerCt) =>
			{
				var result = await instance.Watchdog.Launch(innerCt).ConfigureAwait(false);
				if (result == null)
					throw new JobException("Watchdog already running!");
			},
			cancellationToken).ConfigureAwait(false);
			return Accepted(job.ToApi());
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamDaemonRights.ReadMetadata | DreamDaemonRights.ReadRevision)]
		public override Task<IActionResult> Read(CancellationToken cancellationToken) => ReadImpl(null, cancellationToken);

		/// <summary>
		/// Implementation of <see cref="Read(CancellationToken)"/>
		/// </summary>
		/// <param name="settings">The <see cref="DreamDaemonSettings"/> to operate on if any</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation</returns>
		async Task<IActionResult> ReadImpl(DreamDaemonSettings settings, CancellationToken cancellationToken)
		{
			var instance = instanceManager.GetInstance(Instance);
			var dd = instance.Watchdog;

			var metadata = (AuthenticationContext.GetRight(RightsType.DreamDaemon) & (ulong)DreamDaemonRights.ReadMetadata) != 0;
			var revision = (AuthenticationContext.GetRight(RightsType.DreamDaemon) & (ulong)DreamDaemonRights.ReadRevision) != 0;

			if (settings == null)
			{
				settings = await DatabaseContext.Instances.Where(x => x.Id == Instance.Id).Select(x => x.DreamDaemonSettings).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
				if (settings == default)
					return StatusCode((int)HttpStatusCode.Gone);
			}
			
			var result = new DreamDaemon();
			if (metadata)
			{
				var alphaActive = dd.AlphaIsActive;
				var llp = dd.LastLaunchParameters;
				var rstate = dd.RebootState;
				result.AutoStart = settings.AutoStart;
				result.CurrentPort = alphaActive ? llp?.PrimaryPort : llp?.SecondaryPort;
				result.CurrentSecurity = llp?.SecurityLevel;
				result.CurrentAllowWebclient = llp?.AllowWebClient;
				result.PrimaryPort = settings.PrimaryPort;
				result.AllowWebClient = settings.AllowWebClient;
				result.Running = dd.Running;
				result.SecondaryPort = settings.SecondaryPort;
				result.SecurityLevel = settings.SecurityLevel;
				result.SoftRestart = rstate == RebootState.Restart;
				result.SoftShutdown = rstate == RebootState.Shutdown;
				result.StartupTimeout = settings.StartupTimeout;
			};

			if (revision)
			{
				result.ActiveCompileJob = dd.ActiveCompileJob?.ToApi();
				var compileJob = instance.LatestCompileJob();
				result.StagedCompileJob = compileJob?.ToApi();
			}

			return Json(result);
		}

		/// <inheritdoc />
		[HttpDelete]
		[TgsAuthorize(DreamDaemonRights.Shutdown)]
		public async Task<IActionResult> Delete(CancellationToken cancellationToken)
		{
			//alias for stopping DD
			var instance = instanceManager.GetInstance(Instance);
			await instance.Watchdog.Terminate(false, cancellationToken).ConfigureAwait(false);
			return Ok();
		}

		/// <inheritdoc />
		[TgsAuthorize(DreamDaemonRights.SetAutoStart | DreamDaemonRights.SetPorts | DreamDaemonRights.SetSecurity | DreamDaemonRights.SetWebClient | DreamDaemonRights.SoftRestart | DreamDaemonRights.SoftShutdown | DreamDaemonRights.Start | DreamDaemonRights.SetStartupTimeout)]
		public override async Task<IActionResult> Update([FromBody] DreamDaemon model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.PrimaryPort == 0)
				return BadRequest(new ErrorMessage { Message = "Primary port cannot be 0!" });

			if (model.SecurityLevel == DreamDaemonSecurity.Ultrasafe)
				return BadRequest(new ErrorMessage { Message = "This version of TGS does not support the ultrasafe DreamDaemon configuration!" });

			//alias for changing DD settings
			var current = await DatabaseContext.Instances.Where(x => x.Id == Instance.Id).Select(x => x.DreamDaemonSettings).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

			if (current == default)
				return StatusCode((int)HttpStatusCode.Gone);

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

			if (CheckModified(x => x.AllowWebClient, DreamDaemonRights.SetWebClient)
				|| CheckModified(x => x.AutoStart, DreamDaemonRights.SetAutoStart)
				|| CheckModified(x => x.PrimaryPort, DreamDaemonRights.SetPorts)
				|| CheckModified(x => x.SecondaryPort, DreamDaemonRights.SetPorts)
				|| CheckModified(x => x.SecurityLevel, DreamDaemonRights.SetSecurity)
				|| CheckModified(x => x.SoftRestart, DreamDaemonRights.SoftRestart)
				|| CheckModified(x => x.SoftShutdown, DreamDaemonRights.SoftShutdown)
				|| CheckModified(x => x.StartupTimeout, DreamDaemonRights.SetStartupTimeout))
				return Forbid();

			if (current.PrimaryPort == current.SecondaryPort)
				return BadRequest(new ErrorMessage { Message = "Primary port and secondary port cannot be the same!" });

			var wd = instanceManager.GetInstance(Instance).Watchdog;
			
			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);
			//run this second because current may be modified by it
			await wd.ChangeSettings(current, cancellationToken).ConfigureAwait(false);

			if (!oldSoftRestart.Value && current.SoftRestart.Value)
				await wd.Restart(true, cancellationToken).ConfigureAwait(false);
			else if (!oldSoftShutdown.Value && current.SoftShutdown.Value)
				await wd.Terminate(true, cancellationToken).ConfigureAwait(false);
			else if ((oldSoftRestart.Value && !current.SoftRestart.Value) || (oldSoftShutdown.Value && !current.SoftShutdown.Value))
				await wd.ResetRebootState(cancellationToken).ConfigureAwait(false);

			return await ReadImpl(current, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Handle a HTTP PATCH to the <see cref="DreamDaemonController"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request</returns>
		[HttpPatch]
		[TgsAuthorize(DreamDaemonRights.Restart)]
		public async Task<IActionResult> Restart(CancellationToken cancellationToken)
		{
			var job = new Models.Job
			{
				Instance = Instance,
				CancelRightsType = RightsType.DreamDaemon,
				CancelRight = (ulong)DreamDaemonRights.Shutdown,
				StartedBy = AuthenticationContext.User,
				Description = "Restart Watchdog"
			};

			var watchdog = instanceManager.GetInstance(Instance).Watchdog;

			await jobManager.RegisterOperation(job, (paramJob, databaseContext, progressReporter, ct) => watchdog.Restart(false, ct), cancellationToken).ConfigureAwait(false);
			return Accepted(job.ToApi());
		}
	}
}
