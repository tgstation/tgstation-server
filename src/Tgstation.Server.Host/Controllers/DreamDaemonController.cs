using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils;

#pragma warning disable API1001 // Action method returns a success result without a corresponding ProducesResponseType. Somehow this happens ONLY IN THIS CONTROLLER???

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for managing the <see cref="DreamDaemonResponse"/>.
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
		/// Initializes a new instance of the <see cref="DreamDaemonController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="portAllocator">The value of <see cref="IPortAllocator"/>.</param>
		/// <param name="apiHeaders">The <see cref="IApiHeadersProvider"/> for the <see cref="InstanceRequiredController"/>.</param>
		public DreamDaemonController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			ILogger<DreamDaemonController> logger,
			IInstanceManager instanceManager,
			IJobManager jobManager,
			IPortAllocator portAllocator,
			IApiHeadersProvider apiHeaders)
			: base(
				  databaseContext,
				  authenticationContext,
				  logger,
				  instanceManager,
				  apiHeaders)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.portAllocator = portAllocator ?? throw new ArgumentNullException(nameof(portAllocator));
		}

		/// <summary>
		/// Launches the watchdog.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="202">Watchdog launch started successfully.</response>
		[HttpPut]
		[TgsAuthorize(DreamDaemonRights.Start)]
		[ProducesResponseType(typeof(JobResponse), 202)]
		public ValueTask<IActionResult> Create(CancellationToken cancellationToken)
			=> WithComponentInstance(async instance =>
			{
				if (instance.Watchdog.Status != WatchdogStatus.Offline)
					return Conflict(new ErrorMessageResponse(ErrorCode.WatchdogRunning));

				var job = Job.Create(JobCode.WatchdogLaunch, AuthenticationContext.User, Instance, DreamDaemonRights.Shutdown);
				await jobManager.RegisterOperation(
					job,
					(core, databaseContextFactory, paramJob, progressHandler, innerCt) => core!.Watchdog.Launch(innerCt),
					cancellationToken);
				return Accepted(job.ToApi());
			});

		/// <summary>
		/// Get the watchdog status.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="200">Read <see cref="DreamDaemonResponse"/> information successfully.</response>
		/// <response code="410">The database entity for the requested instance could not be retrieved. The instance was likely detached.</response>
		[HttpGet]
		[TgsAuthorize(DreamDaemonRights.ReadMetadata | DreamDaemonRights.ReadRevision)]
		[ProducesResponseType(typeof(DreamDaemonResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public ValueTask<IActionResult> Read(CancellationToken cancellationToken) => ReadImpl(null, false, cancellationToken);

		/// <summary>
		/// Stops the Watchdog if it's running.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="204">Watchdog terminated.</response>
		[HttpDelete]
		[TgsAuthorize(DreamDaemonRights.Shutdown)]
		[ProducesResponseType(204)]
		public ValueTask<IActionResult> Delete(CancellationToken cancellationToken)
			=> WithComponentInstance(async instance =>
			{
				await instance.Watchdog.Terminate(false, cancellationToken);
				return NoContent();
			});

		/// <summary>
		/// Update watchdog settings to be applied at next server reboot.
		/// </summary>
		/// <param name="model">The <see cref="DreamDaemonRequest"/> with updated settings.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
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
			| DreamDaemonRights.SetHealthCheckInterval
			| DreamDaemonRights.SetTopicTimeout
			| DreamDaemonRights.SetAdditionalParameters
			| DreamDaemonRights.SetVisibility
			| DreamDaemonRights.SetProfiler
			| DreamDaemonRights.SetLogOutput
			| DreamDaemonRights.SetMapThreads
			| DreamDaemonRights.BroadcastMessage
			| DreamDaemonRights.SetMinidumps)]
		[ProducesResponseType(typeof(DreamDaemonResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
#pragma warning disable CA1502 // TODO: Decomplexify
#pragma warning disable CA1506
		public async ValueTask<IActionResult> Update([FromBody] DreamDaemonRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);

			if (model.SoftShutdown == true && model.SoftRestart == true)
				return BadRequest(new ErrorMessageResponse(ErrorCode.GameServerDoubleSoft));

			// alias for changing DD settings
			var current = await DatabaseContext
				.Instances
				.Where(x => x.Id == Instance.Id)
				.Select(x => x.DreamDaemonSettings)
				.FirstOrDefaultAsync(cancellationToken);

			if (current == default)
				return this.Gone();

			if (model.Port.HasValue && model.Port.Value != current.Port!.Value)
			{
				Logger.LogTrace(
					"Triggering port allocator for DD-I:{instanceId} because model port {modelPort} doesn't match DB port {dbPort}...",
					Instance.Id,
					model.Port,
					current.Port);
				var verifiedPort = await portAllocator
					.GetAvailablePort(
						model.Port.Value,
						true,
						cancellationToken);

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

			var ddRights = InstancePermissionSet.DreamDaemonRights!.Value;
			if (CheckModified(x => x.AllowWebClient, DreamDaemonRights.SetWebClient)
				|| CheckModified(x => x.AutoStart, DreamDaemonRights.SetAutoStart)
				|| CheckModified(x => x.Port, DreamDaemonRights.SetPort)
				|| CheckModified(x => x.OpenDreamTopicPort, DreamDaemonRights.SetPort)
				|| CheckModified(x => x.SecurityLevel, DreamDaemonRights.SetSecurity)
				|| CheckModified(x => x.Visibility, DreamDaemonRights.SetVisibility)
				|| (model.SoftRestart.HasValue && !ddRights.HasFlag(DreamDaemonRights.SoftRestart))
				|| (model.SoftShutdown.HasValue && !ddRights.HasFlag(DreamDaemonRights.SoftShutdown))
				|| (!String.IsNullOrWhiteSpace(model.BroadcastMessage) && !ddRights.HasFlag(DreamDaemonRights.BroadcastMessage))
				|| CheckModified(x => x.StartupTimeout, DreamDaemonRights.SetStartupTimeout)
				|| CheckModified(x => x.HealthCheckSeconds, DreamDaemonRights.SetHealthCheckInterval)
				|| CheckModified(x => x.DumpOnHealthCheckRestart, DreamDaemonRights.CreateDump)
				|| CheckModified(x => x.TopicRequestTimeout, DreamDaemonRights.SetTopicTimeout)
				|| CheckModified(x => x.AdditionalParameters, DreamDaemonRights.SetAdditionalParameters)
				|| CheckModified(x => x.StartProfiler, DreamDaemonRights.SetProfiler)
				|| CheckModified(x => x.LogOutput, DreamDaemonRights.SetLogOutput)
				|| CheckModified(x => x.MapThreads, DreamDaemonRights.SetMapThreads)
				|| CheckModified(x => x.Minidumps, DreamDaemonRights.SetMinidumps))
				return Forbid();

			return await WithComponentInstance(
				async instance =>
				{
					var watchdog = instance.Watchdog;
					if (!String.IsNullOrWhiteSpace(model.BroadcastMessage)
						&& !await watchdog.Broadcast(model.BroadcastMessage, cancellationToken))
						return Conflict(new ErrorMessageResponse(ErrorCode.BroadcastFailure));

					await DatabaseContext.Save(cancellationToken);

					// run this second because current may be modified by it
					// slight race condition with request cancellation, but I CANNOT be assed right now
					var rebootRequired = await watchdog.ChangeSettings(current, cancellationToken);

					var rebootState = watchdog.RebootState;
					var oldSoftRestart = rebootState == RebootState.Restart;
					var oldSoftShutdown = rebootState == RebootState.Shutdown;
					if (!oldSoftRestart && model.SoftRestart == true && watchdog.Status == WatchdogStatus.Online)
						await watchdog.Restart(true, cancellationToken);
					else if (!oldSoftShutdown && model.SoftShutdown == true)
						await watchdog.Terminate(true, cancellationToken);
					else if ((oldSoftRestart && model.SoftRestart == false) || (oldSoftShutdown && model.SoftShutdown == false))
						await watchdog.ResetRebootState(cancellationToken);

					return await ReadImpl(current, rebootRequired, cancellationToken);
				});
		}
#pragma warning restore CA1506
#pragma warning restore CA1502

		/// <summary>
		/// Creates a <see cref="JobResponse"/> to restart the Watchdog. It will not start if it wasn't already running.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="202">Restart <see cref="JobResponse"/> started successfully.</response>
		[HttpPatch]
		[TgsAuthorize(DreamDaemonRights.Restart)]
		[ProducesResponseType(typeof(JobResponse), 202)]
		public ValueTask<IActionResult> Restart(CancellationToken cancellationToken)
			=> WithComponentInstance(async instance =>
			{
				var job = Job.Create(JobCode.WatchdogRestart, AuthenticationContext.User, Instance, DreamDaemonRights.Shutdown);

				var watchdog = instance.Watchdog;

				if (watchdog.Status == WatchdogStatus.Offline)
					return Conflict(new ErrorMessageResponse(ErrorCode.WatchdogNotRunning));

				await jobManager.RegisterOperation(
					job,
					(core, paramJob, databaseContextFactory, progressReporter, ct) => core!.Watchdog.Restart(false, ct),
					cancellationToken);
				return Accepted(job.ToApi());
			});

		/// <summary>
		/// Creates a <see cref="JobResponse"/> to generate a DreamDaemon process dump.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="202">Dump <see cref="JobResponse"/> started successfully.</response>
		[HttpPatch(Routes.Diagnostics)]
		[TgsAuthorize(DreamDaemonRights.CreateDump)]
		[ProducesResponseType(typeof(JobResponse), 202)]
		public ValueTask<IActionResult> CreateDump(CancellationToken cancellationToken)
			=> WithComponentInstance(async instance =>
			{
				var job = Job.Create(JobCode.WatchdogDump, AuthenticationContext.User, Instance, DreamDaemonRights.CreateDump);

				var watchdog = instance.Watchdog;

				if (watchdog.Status == WatchdogStatus.Offline)
					return Conflict(new ErrorMessageResponse(ErrorCode.WatchdogNotRunning));

				await jobManager.RegisterOperation(
					job,
					(core, databaseContextFactory, paramJob, progressReporter, ct) => core!.Watchdog.CreateDump(ct),
					cancellationToken);
				return Accepted(job.ToApi());
			});

		/// <summary>
		/// Implementation of <see cref="Read(CancellationToken)"/>.
		/// </summary>
		/// <param name="settings">The <see cref="DreamDaemonSettings"/> to operate on if any.</param>
		/// <param name="knownForcedReboot">If there was a settings change made that forced a switch to <see cref="RebootState.Restart"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
#pragma warning disable CA1502 // TODO: Decomplexify
		ValueTask<IActionResult> ReadImpl(DreamDaemonSettings? settings, bool knownForcedReboot, CancellationToken cancellationToken)
#pragma warning restore CA1502
			=> WithComponentInstance(async instance =>
			{
				var dd = instance.Watchdog;
				var metadata = (AuthenticationContext.GetRight(RightsType.DreamDaemon) & (ulong)DreamDaemonRights.ReadMetadata) != 0;
				var revision = (AuthenticationContext.GetRight(RightsType.DreamDaemon) & (ulong)DreamDaemonRights.ReadRevision) != 0;

				if (settings == null)
				{
					settings = await DatabaseContext
						.Instances
						.Where(x => x.Id == Instance.Id)
						.Select(x => x.DreamDaemonSettings!)
						.FirstOrDefaultAsync(cancellationToken);
					if (settings == default)
						return this.Gone();
				}

				var result = new DreamDaemonResponse();
				if (metadata)
				{
					var alphaActive = dd.AlphaIsActive;
					var llp = dd.LastLaunchParameters;
					var rstate = dd.RebootState;
					result.AutoStart = settings.AutoStart!.Value;
					result.CurrentPort = llp?.Port!.Value;
					result.CurrentTopicPort = llp?.OpenDreamTopicPort;
					result.CurrentSecurity = llp?.SecurityLevel!.Value;
					result.CurrentVisibility = llp?.Visibility!.Value;
					result.CurrentAllowWebclient = llp?.AllowWebClient!.Value;
					result.Port = settings.Port!.Value;
					result.OpenDreamTopicPort = settings.OpenDreamTopicPort;
					result.AllowWebClient = settings.AllowWebClient!.Value;

					var firstIteration = true;
					do
					{
						if (!firstIteration)
						{
							cancellationToken.ThrowIfCancellationRequested();
							await Task.Yield();
						}

						firstIteration = false;
						result.Status = dd.Status;
						result.SessionId = dd.SessionId;
						result.WorldIteration = dd.WorldIteration;
						result.LaunchTime = dd.LaunchTime;
						result.ClientCount = dd.ClientCount;
					}
					while (result.Status == WatchdogStatus.Online && !result.SessionId.HasValue); // this is the one invalid combo, it's not that racy

					result.SecurityLevel = settings.SecurityLevel!.Value;
					result.Visibility = settings.Visibility!.Value;
					result.SoftRestart = rstate == RebootState.Restart;
					result.SoftShutdown = rstate == RebootState.Shutdown;
					result.ImmediateMemoryUsage = dd.MemoryUsage;

					if (rstate == RebootState.Normal && knownForcedReboot)
						result.SoftRestart = true;

					result.StartupTimeout = settings.StartupTimeout!.Value;
					result.HealthCheckSeconds = settings.HealthCheckSeconds!.Value;
					result.DumpOnHealthCheckRestart = settings.DumpOnHealthCheckRestart!.Value;
					result.TopicRequestTimeout = settings.TopicRequestTimeout!.Value;
					result.AdditionalParameters = settings.AdditionalParameters;
					result.StartProfiler = settings.StartProfiler;
					result.LogOutput = settings.LogOutput;
					result.MapThreads = settings.MapThreads;
					result.Minidumps = settings.Minidumps;
				}

				if (revision)
				{
					var latestCompileJob = await instance.LatestCompileJob();
					result.ActiveCompileJob = ((instance.Watchdog.Status != WatchdogStatus.Offline
						? dd.ActiveCompileJob
						: latestCompileJob) ?? latestCompileJob)
						?.ToApi();
					if (latestCompileJob?.Id != result.ActiveCompileJob?.Id)
						result.StagedCompileJob = latestCompileJob?.ToApi();
				}

				return Json(result);
			});
	}
}
