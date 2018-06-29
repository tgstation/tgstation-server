using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
		public InstanceController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IJobManager jobManager, IInstanceManager instanceManager, IIOManager ioManager) : base(databaseContext, authenticationContextFactory)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
		}

		/// <inheritdoc />
		[TgsAuthorize(InstanceManagerRights.Create)]
		public override async Task<IActionResult> Create([FromBody] Api.Models.Instance model, CancellationToken cancellationToken)
		{
			var dirExistsTask = ioManager.DirectoryExists(model.Path, cancellationToken);
			if (await ioManager.FileExists(model.Path, cancellationToken).ConfigureAwait(false) || await dirExistsTask.ConfigureAwait(false))
				return Conflict(new { message = "Path not empty!" });

			var newInstance = new Models.Instance
			{
				ChatSettings = new ChatSettings(),
				ConfigurationAllowed = model.ConfigurationAllowed,
				DreamDaemonSettings = new DreamDaemonSettings(),
				DreamMakerSettings = new DreamMakerSettings(),
				Name = model.Name,
				Online = false,
				Path = model.Path,
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
					DatabaseContext.DreamMakerSettings.Remove(newInstance.DreamMakerSettings);
					DatabaseContext.ChatSettings.Remove(newInstance.ChatSettings);
					DatabaseContext.RepositorySettings.Remove(newInstance.RepositorySettings);
					DatabaseContext.DreamDaemonSettings.Remove(newInstance.DreamDaemonSettings);

					await DatabaseContext.Save(default).ConfigureAwait(false);

					throw;
				}
			}
			catch (Exception e)
			{
				return Conflict(new { message = e.Message });
			}

			model.Online = newInstance.Online;
			model.Id = newInstance.Id;

			return Json(model);
		}

		/// <inheritdoc />
		[TgsAuthorize(InstanceManagerRights.Relocate | InstanceManagerRights.Rename | InstanceManagerRights.SetAutoUpdate | InstanceManagerRights.SetConfiguration | InstanceManagerRights.SetOnline)]
		public override async Task<IActionResult> Update([FromBody] Api.Models.Instance model, CancellationToken cancellationToken)
		{
			var originalModel = await DatabaseContext.Instances.Where(x => x.Id == model.Id).FirstAsync(cancellationToken).ConfigureAwait(false);
			if (originalModel == default(Models.Instance))
				return StatusCode(HttpStatusCode.Gone);

			throw new NotImplementedException();
		}
	}
}
