using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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
		/// Construct a <see cref="InstanceController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		public InstanceController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IJobManager jobManager, IInstanceManager instanceManager, IIOManager ioManager) : base(databaseContext, authenticationContextFactory, false)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
		}

		/// <inheritdoc />
		[TgsAuthorize(InstanceManagerRights.Create)]
		public override async Task<IActionResult> Create([FromBody] Api.Models.Instance model, CancellationToken cancellationToken)
		{

			if (String.IsNullOrWhiteSpace(model.Name))
				return BadRequest(new { message = "name must not be empty!" });

			if(model.Path == null)
				return BadRequest(new { message = "path must not be empty!" });

			model.Path = ioManager.ResolvePath(model.Path);
			var dirExistsTask = ioManager.DirectoryExists(model.Path, cancellationToken);
			if (await ioManager.FileExists(model.Path, cancellationToken).ConfigureAwait(false) || await dirExistsTask.ConfigureAwait(false))
				return Conflict(new { message = "Path not empty!" });

			var newInstance = new Models.Instance
			{
				ConfigurationType = model.ConfigurationType ?? ConfigurationType.Disallowed,
				DreamDaemonSettings = new DreamDaemonSettings(),
				DreamMakerSettings = new DreamMakerSettings(),
				Name = model.Name,
				Online = false,
				Path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? model.Path.ToUpperInvariant() : model.Path,
				RepositorySettings = new RepositorySettings()
			};

			DatabaseContext.Instances.Add(newInstance);
			try
			{
				await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

				try
				{
					//actually reserve it now
					await ioManager.CreateDirectory(model.Path, default).ConfigureAwait(false);
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
			
			return Json(newInstance.ToApi());
		}

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
			var originalModel = await DatabaseContext.Instances.Where(x => x.Id == model.Id).FirstAsync(cancellationToken).ConfigureAwait(false);
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
			if (model.Path != null)
			{
				model.Path = ioManager.ResolvePath(model.Path);
				model.Path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? model.Path.ToUpperInvariant() : model.Path;

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

			if (CheckModified(x => x.AutoUpdateInterval, InstanceManagerRights.SetAutoUpdate)
				|| CheckModified(x => x.ConfigurationType, InstanceManagerRights.SetConfiguration)
				|| CheckModified(x => x.Name, InstanceManagerRights.Rename)
				|| CheckModified(x => x.Online, InstanceManagerRights.SetOnline))
				return Forbid();

			await DatabaseContext.Save(cancellationToken).ConfigureAwait(false);

			if (originalModel.Online.Value && model.Online.Value == false)
				await instanceManager.OfflineInstance(originalModel, cancellationToken).ConfigureAwait(false);
			else if(!originalModel.Online.Value && model.Online.Value == true)
				await instanceManager.OnlineInstance(originalModel, cancellationToken).ConfigureAwait(false);

			if (originalModelPath != null)
				await ioManager.MoveDirectory(originalModelPath, model.Path, cancellationToken).ConfigureAwait(false);

			return Json(originalModel.ToApi());
		}

		[TgsAuthorize]
		public override async Task<IActionResult> List(CancellationToken cancellationToken)
		{
			var instances = await DatabaseContext.Instances.Where(x => x.InstanceUsers.Any(y => y.UserId == AuthenticationContext.User.Id && y.AnyRights)).ToListAsync(cancellationToken).ConfigureAwait(false);
			return Json(instances);
		}
	}
}
