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
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing the <see cref="DreamDaemonResponse"/>
	/// </summary>
	[Route(Routes.DreamDaemon)]
	public sealed class DreamDaemonController : InstanceRequiredController
	{
		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="DreamDaemonController"/>.
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// The <see cref="IPortAllocator"/> for the <see cref="DreamDaemonController"/>.
		/// </summary>
		readonly IPortAllocator portAllocator;

		/// <summary>
		/// Construct a <see cref="DreamDaemonController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="portAllocator">The value of <see cref="IPortAllocator"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		public DreamDaemonController(
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			IJobManager jobManager,
			IInstanceManager instanceManager,
			IPortAllocator portAllocator,
			ILogger<DreamDaemonController> logger)
			: base(
				  instanceManager,
				  databaseContext,
				  authenticationContextFactory,
				  logger)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.portAllocator = portAllocator ?? throw new ArgumentNullException(nameof(portAllocator));
		}

		/// <summary>
		/// Launches the watchdog.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="202">Watchdog launch started successfully.</response>
		[HttpPut]
		[TgsAuthorize(DreamDaemonRights.Start)]
		[ProducesResponseType(typeof(JobResponse), 202)]
		public Task<IActionResult> Create(CancellationToken cancellationToken)
			=> WithComponentInstance(async instance =>
			{
				if (instance.Watchdog.Status != WatchdogStatus.Offline)
					return Conflict(new ErrorMessageResponse(ErrorCode.WatchdogRunning));

				var job = new Job
				{
					Description = "Launch DreamDaemon",
					CancelRight = (ulong)DreamDaemonRights.Shutdown,
					CancelRightsType = RightsType.DreamDaemon,
					Instance = Instance,
					StartedBy = AuthenticationContext.User
				};
				await jobManager.RegisterOperation(
					job,
					(core, databaseContextFactory, paramJob, progressHandler, innerCt) => core.Watchdog.Launch(innerCt),
					cancellationToken)
					.ConfigureAwait(false);
				return Accepted(job.ToApi());
			});

		/// <summary>
		/// Get the watchdog status.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">Read <see cref="DreamDaemonResponse"/> information successfully.</response>
		/// <response code="410">The database entity for the requested instance could not be retrieved. The instance was likely detached.</response>
		[HttpGet]
		[TgsAuthorize(DreamDaemonRights.ReadMetadata | DreamDaemonRights.ReadRevision)]
		[ProducesResponseType(typeof(DreamDaemonResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public Task<IActionResult> Read(CancellationToken cancellationToken) => ReadImpl(null, cancellationToken);

		/// <summary>
		/// Implementation of <see cref="Read(CancellationToken)"/>
		/// </summary>
		/// <param name="settings">The <see cref="DreamDaemonSettings"/> to operate on if any</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation</returns>
		Task<IActionResult> ReadImpl(DreamDaemonSettings settings, CancellationToken cancellationToken)
			=> WithComponentInstance(async instance =>
			{
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

				var result = new DreamDaemonResponse();
				if (metadata)
				{
					var alphaActive = dd.AlphaIsActive;
					var llp = dd.LastLaunchParameters;
					var rstate = dd.RebootState;
					result.AutoStart = settings.AutoStart.Value;
					result.CurrentPort = llp?.Port.Value;
					result.CurrentSecurity = llp?.SecurityLevel.Value;
					result.CurrentAllowWebclient = llp?.AllowWebClient.Value;
					result.Port = settings.Port.Value;
					result.AllowWebClient = settings.AllowWebClient.Value;
					result.Status = dd.Status;
					result.SecurityLevel = settings.SecurityLevel.Value;
					result.SoftRestart = rstate == RebootState.Restart;
					result.SoftShutdown = rstate == RebootState.Shutdown;
					result.StartupTimeout = settings.StartupTimeout.Value;
					result.HeartbeatSeconds = settings.HeartbeatSeconds.Value;
					result.TopicRequestTimeout = settings.TopicRequestTimeout.Value;
					result.AdditionalParameters = settings.AdditionalParameters;
				}

				if (revision)
				{
					var latestCompileJob = instance.LatestCompileJob();
					result.ActiveCompileJob = ((instance.Watchdog.Status != WatchdogStatus.Offline
						? dd.ActiveCompileJob
						: latestCompileJob) ?? latestCompileJob)
						?.ToApi();
					if (latestCompileJob?.Id != result.ActiveCompileJob?.Id)
						result.StagedCompileJob = latestCompileJob?.ToApi();
				}

				return Json(result);
			});

		/// <summary>
		/// Stops the Watchdog if it's running.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="204">Watchdog terminated.</response>
		[HttpDelete]
		[TgsAuthorize(DreamDaemonRights.Shutdown)]
		[ProducesResponseType(204)]
		public Task<IActionResult> Delete(CancellationToken cancellationToken)
			=> WithComponentInstance(async instance =>
			{
				await instance.Watchdog.Terminate(false, cancellationToken).ConfigureAwait(false);
				return NoContent();
			});

		/// <summary>
		/// Update watchdog settings to be applied at next server reboot.
		/// </summary>
		/// <param name="model">The updated <see cref="DreamDaemonResponse"/> settings.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">Settings applied successfully.</response>
		/// <response code="410">The database entity for the requested instance could not be retrieved. The instance was likely detached.</response>
		[HttpPost]
		[TgsAuthorize(
			DreamDaemonRights.SetAutoStart
			| DreamDaemonRights.SetPort
			| DreamDaemonRights.SetSecurity
			| DreamDaemonRights.SetWebClient
			| DreamDaemonRights.SoftRestart
			| DreamDaemonRights.SoftShutdown
			| DreamDaemonRights.Start
			| DreamDaemonRights.SetStartupTimeout
			| DreamDaemonRights.SetHeartbeatInterval
			| DreamDaemonRights.SetTopicTimeout)]
		[ProducesResponseType(typeof(DreamDaemonResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		#pragma warning disable CA1502 // TODO: Decomplexify
		#pragma warning disable CA1506
		public async Task<IActionResult> Update([FromBody] DreamDaemonResponse model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.SoftShutdown == true && model.SoftRestart == true)
				return BadRequest(new ErrorMessageResponse(ErrorCode.DreamDaemonDoubleSoft));

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

			if (model.Port.HasValue && model.Port.Value != current.Port.Value)
			{
				var verifiedPort = await portAllocator
					.GetAvailablePort(
						model.Port.Value,
						true,
						cancellationToken)
					.ConfigureAwait(false);
				if (verifiedPort != model.Port)
					return Conflict(new ErrorMessageResponse(ErrorCode.PortNotAvailable));
			}

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

			return await WithComponentInstance(
				async instance =>
				{
					var watchdog = instance.Watchdog;
					var rebootState = watchdog.RebootState;
					var oldSoftRestart = rebootState == RebootState.Restart;
					var oldSoftShutdown = rebootState == RebootState.Shutdown;

					if (CheckModified(x => x.AllowWebClient, DreamDaemonRights.SetWebClient)
						|| CheckModified(x => x.AutoStart, DreamDaemonRights.SetAutoStart)
						|| CheckModified(x => x.Port, DreamDaemonRights.SetPort)
						|| CheckModified(x => x.SecurityLevel, DreamDaemonRights.SetSecurity)
						|| (model.SoftRestart.HasValue && !AuthenticationContext.InstancePermissionSet.DreamDaemonRights.Value.HasFlag(DreamDaemonRights.SoftRestart))
						|| (model.SoftShutdown.HasValue && !AuthenticationContext.InstancePermissionSet.DreamDaemonRights.Value.HasFlag(DreamDaemonRights.SoftShutdown))
						|| CheckModified(x => x.StartupTimeout, DreamDaemonRights.SetStartupTimeout)
						|| CheckModified(x => x.HeartbeatSeconds, DreamDaemonRights.SetHeartbeatInterval)
						|| CheckModified(x => x.TopicRequestTimeout, DreamDaemonRights.SetTopicTimeout)
						|| CheckModified(x => x.AdditionalParameters, DreamDaemonRights.SetAdditionalParameters))
						return Forbid();

					await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

					// run this second because current may be modified by it
					await watchdog.ChangeSettings(current, cancellationToken).ConfigureAwait(false);

					if (!oldSoftRestart && model.SoftRestart == true && watchdog.Status == WatchdogStatus.Online)
						await watchdog.Restart(true, cancellationToken).ConfigureAwait(false);
					else if (!oldSoftShutdown && model.SoftShutdown == true)
						await watchdog.Terminate(true, cancellationToken).ConfigureAwait(false);
					else if ((oldSoftRestart && model.SoftRestart == false) || (oldSoftShutdown && model.SoftShutdown == false))
						await watchdog.ResetRebootState(cancellationToken).ConfigureAwait(false);

					return await ReadImpl(current, cancellationToken).ConfigureAwait(false);
				})
				.ConfigureAwait(false);
		}
#pragma warning restore CA1506
#pragma warning restore CA1502

		/// <summary>
		/// Creates a <see cref="JobResponse"/> to restart the Watchdog. It will not start if it wasn't already running.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request</returns>
		/// <response code="202">Restart <see cref="JobResponse"/> started successfully.</response>
		[HttpPatch]
		[TgsAuthorize(DreamDaemonRights.Restart)]
		[ProducesResponseType(typeof(JobResponse), 202)]
		public Task<IActionResult> Restart(CancellationToken cancellationToken)
			=> WithComponentInstance(async instance =>
			{
				var job = new Job
				{
					Instance = Instance,
					CancelRightsType = RightsType.DreamDaemon,
					CancelRight = (ulong)DreamDaemonRights.Shutdown,
					StartedBy = AuthenticationContext.User,
					Description = "Restart Watchdog"
				};

				var watchdog = instance.Watchdog;

				if (watchdog.Status == WatchdogStatus.Offline)
					return Conflict(new ErrorMessageResponse(ErrorCode.WatchdogNotRunning));

				await jobManager.RegisterOperation(
					job,
					(core, paramJob, databaseContextFactory, progressReporter, ct) => core.Watchdog.Restart(false, ct),
					cancellationToken)
					.ConfigureAwait(false);
				return Accepted(job.ToApi());
			});

		/// <summary>
		/// Creates a <see cref="JobResponse"/> to generate a DreamDaemon process dump.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the request</returns>
		/// <response code="202">Dump <see cref="JobResponse"/> started successfully.</response>
		[HttpPatch(Routes.Diagnostics)]
		[TgsAuthorize(DreamDaemonRights.CreateDump)]
		[ProducesResponseType(typeof(JobResponse), 202)]
		public Task<IActionResult> CreateDump(CancellationToken cancellationToken)
			=> WithComponentInstance(async instance =>
			{
				var job = new Job
				{
					Instance = Instance,
					CancelRightsType = RightsType.DreamDaemon,
					CancelRight = (ulong)DreamDaemonRights.CreateDump,
					StartedBy = AuthenticationContext.User,
					Description = "Create DreamDaemon Process Dump"
				};

				var watchdog = instance.Watchdog;

				if (watchdog.Status == WatchdogStatus.Offline)
					return Conflict(new ErrorMessageResponse(ErrorCode.WatchdogNotRunning));

				await jobManager.RegisterOperation(
					job,
					(core, databaseContextFactory, paramJob, progressReporter, ct) => core.Watchdog.CreateDump(ct), cancellationToken)
					.ConfigureAwait(false);
				return Accepted(job.ToApi());
			});
	}
}
