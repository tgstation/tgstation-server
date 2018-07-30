using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Controller for managing <see cref="Components.Instance"/>s
	/// </summary>
	[Route("/Instance")]
	public sealed class InstanceController : ModelController<Api.Models.Instance>
	{
		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="InstanceController"/>
		/// </summary>
		readonly IJobManager jobManager;
		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="InstanceController"/>
		/// </summary>
		readonly IInstanceManager instanceManager;
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="InstanceController"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IApplication"/> for the <see cref="InstanceController"/>
		/// </summary>
		readonly IApplication application;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="InstanceController"/>
		/// </summary>
		readonly ILogger<InstanceController> logger;

		/// <summary>
		/// Construct a <see cref="InstanceController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="application">The value of <see cref="application"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public InstanceController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IJobManager jobManager, IInstanceManager instanceManager, IIOManager ioManager, IApplication application, ILogger<InstanceController> logger) : base(databaseContext, authenticationContextFactory, false)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.application = application ?? throw new ArgumentNullException(nameof(application));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		void NormalizeModelPath(Api.Models.Instance model, out string absolutePath)
		{
			if (model.Path == null)
			{
				absolutePath = null;
				return;
			}
			absolutePath = ioManager.ResolvePath(model.Path);
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				model.Path = absolutePath.ToUpperInvariant();
			else
				model.Path = absolutePath;
		}

		Models.InstanceUser InstanceAdminUser() => new Models.InstanceUser
		{
			ByondRights = (ByondRights)~0,
			ChatSettingsRights = (ChatSettingsRights)~0,
			ConfigurationRights = (ConfigurationRights)~0,
			DreamDaemonRights = (DreamDaemonRights)~0,
			DreamMakerRights = (DreamMakerRights)~0,
			RepositoryRights = (RepositoryRights)~0,
			InstanceUserRights = (InstanceUserRights)~0,
			UserId = AuthenticationContext.User.Id
		};

		/// <inheritdoc />
		[TgsAuthorize(InstanceManagerRights.Create)]
		public override async Task<IActionResult> Create([FromBody] Api.Models.Instance model, CancellationToken cancellationToken)
		{
			if (String.IsNullOrWhiteSpace(model.Name))
				return BadRequest(new { message = "name must not be empty!" });

			if(model.Path == null)
				return BadRequest(new { message = "path must not be empty!" });

			NormalizeModelPath(model, out var rawPath);
			var dirExistsTask = ioManager.DirectoryExists(model.Path, cancellationToken);
			if (await ioManager.FileExists(model.Path, cancellationToken).ConfigureAwait(false) || await dirExistsTask.ConfigureAwait(false))
				return Conflict(new { message = "Path not empty!" });

			var newInstance = new Models.Instance
			{
				ConfigurationType = model.ConfigurationType ?? ConfigurationType.Disallowed,
				DreamDaemonSettings = new DreamDaemonSettings
				{
					AllowWebClient = false,
					AutoStart = false,
					PrimaryPort = 1337,
					SecondaryPort = 1338,
					SecurityLevel = DreamDaemonSecurity.Ultrasafe,
					SoftRestart = false,
					SoftShutdown = false,
					StartupTimeout = 20
				},
				DreamMakerSettings = new DreamMakerSettings(),
				Name = model.Name,
				Online = false,
				Path = model.Path,
				RepositorySettings = new RepositorySettings
				{
					CommitterEmail = "tgstation-server@user.noreply.github.com",
					CommitterName = application.VersionString,
					PushTestMergeCommits = false,
					ShowTestMergeCommitters = true,
					AutoUpdatesKeepTestMerges = false,
					AutoUpdatesSynchronize = false
				},
				//give this user full privileges on the instance
				InstanceUsers = new List<Models.InstanceUser>
				{
					InstanceAdminUser()
				}
			};

			DatabaseContext.Instances.Add(newInstance);
			try
			{
				await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

				try
				{
					//actually reserve it now
					await ioManager.CreateDirectory(rawPath, default).ConfigureAwait(false);
				}
				catch
				{
					//oh shit delete the model
					DatabaseContext.Instances.Remove(newInstance);

					await DatabaseContext.Save(default).ConfigureAwait(false);

					throw;
				}
			}
			catch (DbUpdateConcurrencyException e)
			{
				return Conflict(new { message = e.Message });
			}

			logger.LogInformation("{0} created instance {1}: {2}", AuthenticationContext.User.Name, newInstance.Name, newInstance.Id);
			
			return Json(newInstance.ToApi());
		}

		/// <inheritdoc />
		[TgsAuthorize(InstanceManagerRights.Delete)]
		public override async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
		{
			var originalModel = await DatabaseContext.Instances.Where(x => x.Id == id)
				.Include(x => x.WatchdogReattachInformation)
				.Include(x => x.WatchdogReattachInformation.Alpha)
				.Include(x => x.WatchdogReattachInformation.Bravo)
				.FirstAsync(cancellationToken).ConfigureAwait(false);
			if (originalModel == default(Models.Instance))
				return StatusCode((int)HttpStatusCode.Gone);

			if (originalModel.WatchdogReattachInformation != null)
			{
				DatabaseContext.WatchdogReattachInformations.Remove(originalModel.WatchdogReattachInformation);
				if (originalModel.WatchdogReattachInformation.Alpha != null)
					DatabaseContext.ReattachInformations.Remove(originalModel.WatchdogReattachInformation.Alpha);
				if (originalModel.WatchdogReattachInformation.Bravo != null)
					DatabaseContext.ReattachInformations.Remove(originalModel.WatchdogReattachInformation.Bravo);
			}

			DatabaseContext.Instances.Remove(originalModel);
			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);	//cascades everything
			return Ok();
		}

		/// <inheritdoc />
		[TgsAuthorize(InstanceManagerRights.Relocate | InstanceManagerRights.Rename | InstanceManagerRights.SetAutoUpdate | InstanceManagerRights.SetConfiguration | InstanceManagerRights.SetOnline)]
		public override async Task<IActionResult> Update([FromBody] Api.Models.Instance model, CancellationToken cancellationToken)
		{
			var instanceQuery = DatabaseContext.Instances.Where(x => x.Id == model.Id);

			var usersInstanceUserTask = instanceQuery
				.Include(x => x.InstanceUsers)
				.SelectMany(x => x.InstanceUsers).Where(x => x.UserId == AuthenticationContext.User.Id).FirstOrDefaultAsync(cancellationToken);

			var originalModel = await instanceQuery
				.Include(x => x.RepositorySettings)
				.Include(x => x.ChatSettings)
				.ThenInclude(x => x.Channels)
				.Include(x => x.DreamDaemonSettings)    //need these for onlining
				.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (originalModel == default(Models.Instance))
				return StatusCode((int)HttpStatusCode.Gone);

			var userRights = (InstanceManagerRights)AuthenticationContext.GetRight(RightsType.InstanceManager);
			bool CheckModified<T>(Expression<Func<Api.Models.Instance, T>> expression, InstanceManagerRights requiredRight)
			{
				var memberSelectorExpression = (MemberExpression)expression.Body;
				var property = (PropertyInfo)memberSelectorExpression.Member;

				var newVal = property.GetValue(model);
				if (newVal == null)
					return false;
				if (!userRights.HasFlag(requiredRight) && property.GetValue(originalModel) != newVal)
					return true;

				property.SetValue(originalModel, newVal);
				return false;
			};

			string originalModelPath = null;
			string rawPath = null;
			if (model.Path != null)
			{
				NormalizeModelPath(model, out rawPath);

				if (model.Path != originalModel.Path)
				{
					if (!userRights.HasFlag(InstanceManagerRights.Relocate))
						return Forbid();
					if (originalModel.Online.Value && model.Online != true)
						return Conflict(new { message = "Cannot relocate an online instance!" });

					var dirExistsTask = ioManager.DirectoryExists(model.Path, cancellationToken);
					if (await ioManager.FileExists(model.Path, cancellationToken).ConfigureAwait(false) || await dirExistsTask.ConfigureAwait(false))
						return Conflict(new { message = "Path not empty!" });

					originalModelPath = originalModel.Path;
					originalModel.Path = model.Path;
				}
			}

			var originalOnline = originalModel.Online.Value;

			if (CheckModified(x => x.AutoUpdateInterval, InstanceManagerRights.SetAutoUpdate)
				|| CheckModified(x => x.ConfigurationType, InstanceManagerRights.SetConfiguration)
				|| CheckModified(x => x.Name, InstanceManagerRights.Rename)
				|| CheckModified(x => x.Online, InstanceManagerRights.SetOnline))
				return Forbid();

			//ensure the current user has write privilege on the instance
			var usersInstanceUser = await usersInstanceUserTask.ConfigureAwait(false);
			if (usersInstanceUser == default)
				originalModel.InstanceUsers.Add(InstanceAdminUser());
			else
				usersInstanceUser.InstanceUserRights |= InstanceUserRights.WriteUsers;

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			var oldAutoStart = originalModel.DreamDaemonSettings.AutoStart;
			try
			{
				if (originalOnline && model.Online.Value == false)
					await instanceManager.OfflineInstance(originalModel, cancellationToken).ConfigureAwait(false);
				else if (!originalOnline && model.Online.Value == true)
				{
					//force autostart false here because we don't want any long running jobs right now
					//remember to document this
					originalModel.DreamDaemonSettings.AutoStart = false;
					await instanceManager.OnlineInstance(originalModel, cancellationToken).ConfigureAwait(false);
				}
			}
			catch (Exception e)
			{
				logger.LogError("Error changing instance online state! Exception: {0}", e);
				originalModel.Online = originalOnline;
				originalModel.DreamDaemonSettings.AutoStart = oldAutoStart;
				if (originalModelPath != null)
					originalModel.Path = originalModelPath;
				await DatabaseContext.Save(default).ConfigureAwait(false);
				throw;
			}

			var api = originalModel.ToApi();
			if (originalModelPath != null)
			{
				var job = new Models.Job
				{
					Description = String.Format(CultureInfo.InvariantCulture, "Move instance ID {0} from {1} to {2}", Instance.Id, Instance.Path, rawPath),
					Instance = Instance,
					CancelRightsType = RightsType.InstanceManager,
					CancelRight = (int)InstanceManagerRights.CancelMove,
					StartedBy = AuthenticationContext.User
				};

				await jobManager.RegisterOperation(job, async (paramJob, serviceProvider, ct) => {
					try
					{
						await instanceManager.MoveInstance(Instance, rawPath, ct).ConfigureAwait(false);
					}
					catch
					{
						originalModel.Path = originalModelPath;
						await DatabaseContext.Save(default).ConfigureAwait(false);
						throw;
					}
				}, cancellationToken).ConfigureAwait(false);
				api.MoveJob = job.ToApi();
			}

			return Json(api);
		}

		/// <inheritdoc />
		[TgsAuthorize]
		public override async Task<IActionResult> List(CancellationToken cancellationToken)
		{
			IQueryable<Models.Instance> query = DatabaseContext.Instances;
			if (!AuthenticationContext.User.InstanceManagerRights.Value.HasFlag(InstanceManagerRights.List))
				query = query.Where(x => x.InstanceUsers.Any(y => y.UserId == AuthenticationContext.User.Id)).Where(x => x.InstanceUsers.Any(y => y.AnyRights));
			var instances = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
			return Json(instances.Select(x => x.ToApi()));
		}

		/// <inheritdoc />
		[TgsAuthorize]
		public override Task<IActionResult> Read(CancellationToken cancellationToken)
		{
			if (Instance == null)
				return Task.FromResult<IActionResult>(BadRequest(new { message = "No instance specified" }));
			return Task.FromResult<IActionResult>(Json(Instance.ToApi()));
		}
	}
}
