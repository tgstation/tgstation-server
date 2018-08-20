using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Linq;
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
	/// Controller for managing <see cref="Api.Models.Byond.Version"/>s
	/// </summary>
	[Route(Routes.Byond)]
	public sealed class ByondController : ModelController<Api.Models.Byond>
	{
		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="ByondController"/>
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="ByondController"/>
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// Construct a <see cref="ByondController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/></param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		public ByondController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IInstanceManager instanceManager, IJobManager jobManager, ILogger<ByondController> logger) : base(databaseContext, authenticationContextFactory, logger, true)
		{
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
		}

		/// <inheritdoc />
		[TgsAuthorize(ByondRights.ReadActive)]
		public override Task<IActionResult> Read(CancellationToken cancellationToken) => Task.FromResult((IActionResult)
			Json(new Api.Models.Byond
			{
				Version = instanceManager.GetInstance(Instance).ByondManager.ActiveVersion
			}));

		/// <inheritdoc />
		[TgsAuthorize(ByondRights.ListInstalled)]
		public override Task<IActionResult> List(CancellationToken cancellationToken) => Task.FromResult((IActionResult)
			Json(instanceManager.GetInstance(Instance).ByondManager.InstalledVersions.Select(x => new Api.Models.Byond
			{
				Version = x
			})));

		/// <inheritdoc />
		[TgsAuthorize(ByondRights.ChangeVersion)]
		public override async Task<IActionResult> Update([FromBody] Api.Models.Byond model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.Version == null)
				return BadRequest(new ErrorMessage { Message = "Missing version!" });

			var byondManager = instanceManager.GetInstance(Instance).ByondManager;

			//remove cruff fields
			var installingVersion = new Version(model.Version.Major, model.Version.Minor);

			var result = new Api.Models.Byond();

			if (byondManager.InstalledVersions.Any(x => x == model.Version))
			{
				Logger.LogInformation("User ID {0} changing instance ID {1} BYOND version to {2}", AuthenticationContext.User.Id, Instance.Id, installingVersion);
				await byondManager.ChangeVersion(model.Version, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				Logger.LogInformation("User ID {0} installing BYOND version to {2} on instance ID {1}", AuthenticationContext.User.Id, Instance.Id, installingVersion);
				//run the install through the job manager
				var job = new Models.Job
				{
					Description = String.Format(CultureInfo.InvariantCulture, "Install BYOND version {0}", installingVersion),
					StartedBy = AuthenticationContext.User,
					CancelRightsType = RightsType.Byond,
					CancelRight = (ulong)ByondRights.CancelInstall,
					Instance = Instance
				};
				await jobManager.RegisterOperation(job, (paramJob, serviceProvicer, progressHandler, ct) => byondManager.ChangeVersion(installingVersion, ct), cancellationToken).ConfigureAwait(false);
				result.InstallJob = job.ToApi();
			}
			result.Version = byondManager.ActiveVersion;
			return result.InstallJob != null ? (IActionResult)Accepted(result) : Json(result);
		}
	}
}
