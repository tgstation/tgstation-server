using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing the <see cref="DreamDaemon"/>
	/// </summary>
	[Route(Routes.DreamDaemon)]
	public sealed class DreamDaemonController : ApiController
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
		public DreamDaemonController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IJobManager jobManager, IInstanceManager instanceManager, ILogger<DreamDaemonController> logger) : base(databaseContext, authenticationContextFactory, logger, true, true)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
		}

		/// <summary>
		/// Launches the watchdog.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="202"><see cref="Api.Models.Job"/> to launch the watchdog started successfully.</response>
		[HttpPut]
		[TgsAuthorize(DreamDaemonRights.Start)]
		[ProducesResponseType(typeof(Api.Models.Job), 202)]
		public async Task<IActionResult> Create(CancellationToken cancellationToken)
		{
			// alias for launching DD
			var instance = instanceManager.GetInstance(Instance);

			if (instance.Watchdog.Running)
				return Conflict(new ErrorMessage(ErrorCode.WatchdogRunning));

			var job = new Models.Job
			{
				Description = "Launch DreamDaemon",
				CancelRight = (ulong)DreamDaemonRights.Shutdown,
				CancelRightsType = RightsType.DreamDaemon,
				Instance = Instance,
				StartedBy = AuthenticationContext.User
			};
			await jobManager.RegisterOperation(job, (paramJob, databaseContextFactory, progressHandler, innerCt) => instance.Watchdog.Launch(innerCt), cancellationToken).ConfigureAwait(false);
			return Accepted(job.ToApi());
		}

		/// <summary>
		/// Get the watchdog status.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">Read <see cref="DreamDaemon"/> information successfully.</response>
		/// <response code="410">The database entity for the requested instance could not be retrieved. The instance was likely detached.</response>
		[HttpGet]
		[TgsAuthorize(DreamDaemonRights.ReadMetadata | DreamDaemonRights.ReadRevision)]
		[ProducesResponseType(typeof(DreamDaemon), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 410)]
		public Task<IActionResult> Read(CancellationToken cancellationToken) => ReadImpl(null, cancellationToken);

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
				settings = await DatabaseContext
					.Instances
					.AsQueryable()
					.Where(x => x.Id == Instance.Id)
					.Select(x => x.DreamDaemonSettings)
					.FirstOrDefaultAsync(cancellationToken)
					.ConfigureAwait(false);
				if (settings == default)
					return Gone();
			}

			var result = new DreamDaemon();
			if (metadata)
			{
				var alphaActive = dd.AlphaIsActive;
				var llp = dd.LastLaunchParameters;
				var rstate = dd.RebootState;
				result.AutoStart = settings.AutoStart.Value;
				result.CurrentPort = alphaActive ? llp?.PrimaryPort.Value : llp?.SecondaryPort.Value;
				result.CurrentSecurity = llp?.SecurityLevel.Value;
				result.CurrentAllowWebclient = llp?.AllowWebClient.Value;
				result.PrimaryPort = settings.PrimaryPort.Value;
				result.AllowWebClient = settings.AllowWebClient.Value;
				result.Running = dd.Running;
				result.SecondaryPort = settings.SecondaryPort.Value;
				result.SecurityLevel = settings.SecurityLevel.Value;
				result.SoftRestart = rstate == RebootState.Restart;
				result.SoftShutdown = rstate == RebootState.Shutdown;
				result.StartupTimeout = settings.StartupTimeout.Value;
				result.HeartbeatSeconds = settings.HeartbeatSeconds.Value;
			}

			if (revision)
			{
				var latestCompileJob = instance.LatestCompileJob();
				result.ActiveCompileJob = ((dd.Running ? dd.ActiveCompileJob : latestCompileJob) ?? latestCompileJob)?.ToApi();
				if (latestCompileJob?.Id != result.ActiveCompileJob?.Id)
					result.StagedCompileJob = latestCompileJob?.ToApi();
			}

			return Json(result);
		}

		/// <summary>
		/// Stops the Watchdog if it's running.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="204">Watchdog terminated.</response>
		[HttpDelete]
		[TgsAuthorize(DreamDaemonRights.Shutdown)]
		[ProducesResponseType(204)]
		public async Task<IActionResult> Delete(CancellationToken cancellationToken)
		{
			var instance = instanceManager.GetInstance(Instance);
			await instance.Watchdog.Terminate(false, cancellationToken).ConfigureAwait(false);
			return NoContent();
		}

		/// <summary>
		/// Update watchdog settings to be applied at next server reboot.
		/// </summary>
		/// <param name="model">The updated <see cref="DreamDaemon"/> settings.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">Settings applied successfully.</response>
		/// <response code="410">The database entity for the requested instance could not be retrieved. The instance was likely detached.</response>
		[HttpPost]
		[TgsAuthorize(DreamDaemonRights.SetAutoStart | DreamDaemonRights.SetPorts | DreamDaemonRights.SetSecurity | DreamDaemonRights.SetWebClient | DreamDaemonRights.SoftRestart | DreamDaemonRights.SoftShutdown | DreamDaemonRights.Start | DreamDaemonRights.SetStartupTimeout | DreamDaemonRights.SetHeartbeatInterval)]
		[ProducesResponseType(typeof(DreamDaemon), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 410)]
		#pragma warning disable CA1502 // TODO: Decomplexify
		#pragma warning disable CA1506
		public async Task<IActionResult> Update([FromBody] DreamDaemon model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.PrimaryPort == 0)
				throw new InvalidOperationException("Primary port cannot be 0!");

			if (model.SecondaryPort == 0)
				throw new InvalidOperationException("Secondary port cannot be 0!");

			if (model.SoftShutdown == true && model.SoftRestart == true)
				return BadRequest(new ErrorMessage(ErrorCode.DreamDaemonDoubleSoft));

			// alias for changing DD settings
			var current = await DatabaseContext
				.Instances
				.AsQueryable()
				.Where(x => x.Id == Instance.Id)
				.Select(x => x.DreamDaemonSettings)
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);

			if (current == default)
				return Gone();

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
			}

			var instance = instanceManager.GetInstance(Instance);
			var dd = instance.Watchdog;
			var rebootState = dd.RebootState;
			var oldSoftRestart = rebootState == RebootState.Restart;
			var oldSoftShutdown = rebootState == RebootState.Shutdown;

			if (CheckModified(x => x.AllowWebClient, DreamDaemonRights.SetWebClient)
				|| CheckModified(x => x.AutoStart, DreamDaemonRights.SetAutoStart)
				|| CheckModified(x => x.PrimaryPort, DreamDaemonRights.SetPorts)
				|| CheckModified(x => x.SecondaryPort, DreamDaemonRights.SetPorts)
				|| CheckModified(x => x.SecurityLevel, DreamDaemonRights.SetSecurity)
				|| (model.SoftRestart.HasValue && !AuthenticationContext.InstanceUser.DreamDaemonRights.Value.HasFlag(DreamDaemonRights.SoftRestart))
				|| (model.SoftShutdown.HasValue && !AuthenticationContext.InstanceUser.DreamDaemonRights.Value.HasFlag(DreamDaemonRights.SoftShutdown))
				|| CheckModified(x => x.StartupTimeout, DreamDaemonRights.SetStartupTimeout)
				|| CheckModified(x => x.HeartbeatSeconds, DreamDaemonRights.SetHeartbeatInterval))
				return Forbid();

			if (current.PrimaryPort == current.SecondaryPort)
				return BadRequest(new ErrorMessage(ErrorCode.DreamDaemonDuplicatePorts));

			var wd = instanceManager.GetInstance(Instance).Watchdog;

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			// run this second because current may be modified by it
			await wd.ChangeSettings(current, cancellationToken).ConfigureAwait(false);

			if (!oldSoftRestart && model.SoftRestart == true)
				await wd.Restart(true, cancellationToken).ConfigureAwait(false);
			else if (!oldSoftShutdown && model.SoftShutdown == true)
				await wd.Terminate(true, cancellationToken).ConfigureAwait(false);
			else if ((oldSoftRestart && model.SoftRestart == false) || (oldSoftShutdown && model.SoftShutdown == false))
				await wd.ResetRebootState(cancellationToken).ConfigureAwait(false);

			return await ReadImpl(current, cancellationToken).ConfigureAwait(false);
		}
		#pragma warning restore CA1506
		#pragma warning restore CA1502

		/// <summary>
		/// Creates a <see cref="Api.Models.Job"/> to restart the Watchdog. It will start if it wasn't already running.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request</returns>
		/// <response code="202">Restart <see cref="Api.Models.Job"/> started successfully.</response>
		[HttpPatch]
		[TgsAuthorize(DreamDaemonRights.Restart)]
		[ProducesResponseType(typeof(Api.Models.Job), 202)]
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

			if (!watchdog.Running)
				return Conflict(new ErrorMessage(ErrorCode.WatchdogNotRunning));

			await jobManager.RegisterOperation(job, (paramJob, databaseContextFactory, progressReporter, ct) => watchdog.Restart(false, ct), cancellationToken).ConfigureAwait(false);
			return Accepted(job.ToApi());
		}

		/// <summary>
		/// Creates a <see cref="Api.Models.Job"/> to generate a DreamDaemon process dump.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request</returns>
		/// <response code="202">Dump <see cref="Api.Models.Job"/> started successfully.</response>
		[HttpPatch(Routes.Diagnostics)]
		[TgsAuthorize(DreamDaemonRights.CreateDump)]
		[ProducesResponseType(typeof(Api.Models.Job), 202)]
		public async Task<IActionResult> CreateDump(CancellationToken cancellationToken)
		{
			var job = new Models.Job
			{
				Instance = Instance,
				CancelRightsType = RightsType.DreamDaemon,
				CancelRight = (ulong)DreamDaemonRights.CreateDump,
				StartedBy = AuthenticationContext.User,
				Description = "Create DreamDaemon Process Dump"
			};

			var watchdog = instanceManager.GetInstance(Instance).Watchdog;

			if (!watchdog.Running)
				return Conflict(new ErrorMessage(ErrorCode.WatchdogNotRunning));

			await jobManager.RegisterOperation(
				job,
				(paramJob, databaseContextFactory, progressReporter, ct) => watchdog.CreateDump(ct), cancellationToken)
				.ConfigureAwait(false);
			return Accepted(job.ToApi());
		}
	}
}
