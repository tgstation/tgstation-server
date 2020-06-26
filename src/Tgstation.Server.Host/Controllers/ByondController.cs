using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Controller for managing <see cref="Api.Models.Byond.Version"/>s
	/// </summary>
	[Route(Routes.Byond)]
	public sealed class ByondController : ApiController
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
		public ByondController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IInstanceManager instanceManager, IJobManager jobManager, ILogger<ByondController> logger) : base(databaseContext, authenticationContextFactory, logger, true, true)
		{
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
		}

		/// <summary>
		/// Gets the active <see cref="Api.Models.Byond"/> version.
		/// </summary>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Retrieved version information successfully.</response>
		[HttpGet]
		[TgsAuthorize(ByondRights.ReadActive)]
		[ProducesResponseType(typeof(Api.Models.Byond), 200)]
		public Task<IActionResult> Read() => Task.FromResult<IActionResult>(
			Json(new Api.Models.Byond
			{
				Version = instanceManager.GetInstance(Instance).ByondManager.ActiveVersion
			}));

		/// <summary>
		/// Lists installed <see cref="Api.Models.Byond"/> versions.
		/// </summary>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Retrieved version information successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(ByondRights.ListInstalled)]
		[ProducesResponseType(typeof(IEnumerable<Api.Models.Byond>), 200)]
		public IActionResult List()
			=> Json(
				instanceManager
					.GetInstance(Instance)
					.ByondManager
					.InstalledVersions
					.Select(x => new Api.Models.Byond
					{
						Version = x
					}));

		/// <summary>
		/// Changes the active BYOND version to the one specified in a given <paramref name="model"/>.
		/// </summary>
		/// <param name="model">The <see cref="Api.Models.Byond.Version"/> to switch to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Switched active version successfully.</response>
		/// <response code="202">Created <see cref="Api.Models.Job"/> to install and switch active version successfully.</response>
		[HttpPost]
		[TgsAuthorize(ByondRights.ChangeVersion)]
		[ProducesResponseType(typeof(Api.Models.Byond), 200)]
		[ProducesResponseType(typeof(Api.Models.Byond), 202)]
		public async Task<IActionResult> Update([FromBody] Api.Models.Byond model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.Version == null
				|| model.Version.Revision != -1
				|| (model.Content != null && model.Version.Build > 0))
				return BadRequest(new ErrorMessage(ErrorCode.ModelValidationFailure));

			var byondManager = instanceManager.GetInstance(Instance).ByondManager;

			// remove cruff fields
			var result = new Api.Models.Byond();

			if (model.Content == null && byondManager.InstalledVersions.Any(x => x == model.Version))
			{
				Logger.LogInformation(
					"User ID {0} changing instance ID {1} BYOND version to {2}",
					AuthenticationContext.User.Id,
					Instance.Id,
					model.Version);
				await byondManager.ChangeVersion(model.Version, null, cancellationToken).ConfigureAwait(false);
			}
			else if (model.Version.Build > 0)
				return BadRequest(new ErrorMessage(ErrorCode.ByondNonExistentCustomVersion));
			else
			{
				var installingVersion = model.Version.Build <= 0
					? new Version(model.Version.Major, model.Version.Minor)
					: model.Version;

				Logger.LogInformation(
					"User ID {0} installing BYOND version to {1} on instance ID {2}",
					AuthenticationContext.User.Id,
					installingVersion,
					Instance.Id);

				// run the install through the job manager
				var job = new Models.Job
				{
					Description = $"Install BYOND version {model.Version}",
					StartedBy = AuthenticationContext.User,
					CancelRightsType = RightsType.Byond,
					CancelRight = (ulong)ByondRights.CancelInstall,
					Instance = Instance
				};
				await jobManager.RegisterOperation(
					job,
					(paramJob, databaseContextFactory, progressHandler, jobCancellationToken) => byondManager.ChangeVersion(
						model.Version,
						model.Content,
						jobCancellationToken),
					cancellationToken)
					.ConfigureAwait(false);
				result.InstallJob = job.ToApi();
			}

			if ((AuthenticationContext.GetRight(RightsType.Byond) & (ulong)ByondRights.ReadActive) != 0)
				result.Version = byondManager.ActiveVersion;
			return result.InstallJob != null ? (IActionResult)Accepted(result) : Json(result);
		}
	}
}
