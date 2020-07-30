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
	public sealed class ByondController : InstanceRequiredController
	{
		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="ByondController"/>
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// Construct a <see cref="ByondController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="jobManager">The value of <see cref="jobManager"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		public ByondController(
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			IInstanceManager instanceManager,
			IJobManager jobManager,
			ILogger<ByondController> logger)
			: base(
				  instanceManager,
				  databaseContext,
				  authenticationContextFactory,
				  logger)
		{
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
		public Task<IActionResult> Read()
			=> WithComponentInstance(instance =>
				Task.FromResult<IActionResult>(
					Json(new Api.Models.Byond
					{
						Version = instance.ByondManager.ActiveVersion
					})));

		/// <summary>
		/// Lists installed <see cref="Api.Models.Byond"/> versions.
		/// </summary>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Retrieved version information successfully.</response>
		[HttpGet(Routes.List)]
		[TgsAuthorize(ByondRights.ListInstalled)]
		[ProducesResponseType(typeof(IEnumerable<Api.Models.Byond>), 200)]
		public Task<IActionResult> List()
			=> WithComponentInstance(instance =>
				Task.FromResult<IActionResult>(
					Json(instance
						.ByondManager
						.InstalledVersions
						.Select(x => new Api.Models.Byond
						{
							Version = x
						}))));

		/// <summary>
		/// Changes the active BYOND version to the one specified in a given <paramref name="model"/>.
		/// </summary>
		/// <param name="model">The <see cref="Api.Models.Byond.Version"/> to switch to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Switched active version successfully.</response>
		/// <response code="202">Created <see cref="Api.Models.Job"/> to install and switch active version successfully.</response>
		[HttpPost]
		[TgsAuthorize(ByondRights.InstallOfficialOrChangeActiveVersion | ByondRights.InstallCustomVersion)]
		[ProducesResponseType(typeof(Api.Models.Byond), 200)]
		[ProducesResponseType(typeof(Api.Models.Byond), 202)]
#pragma warning disable CA1506 // TODO: Decomplexify
		public async Task<IActionResult> Update([FromBody] Api.Models.Byond model, CancellationToken cancellationToken)
#pragma warning restore CA1506
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (model.Version == null
				|| model.Version.Revision != -1
				|| (model.Content != null && model.Version.Build > 0))
				return BadRequest(new ErrorMessage(ErrorCode.ModelValidationFailure));

			var userByondRights = AuthenticationContext.InstanceUser.ByondRights.Value;
			if ((!userByondRights.HasFlag(ByondRights.InstallOfficialOrChangeActiveVersion) && model.Content == null)
				|| (!userByondRights.HasFlag(ByondRights.InstallCustomVersion) && model.Content != null))
				return Forbid();

			// remove cruff fields
			var result = new Api.Models.Byond();
			return await WithComponentInstance(
				async instance =>
				{
					var byondManager = instance.ByondManager;
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
							Description = $"Install {(model.Content == null ? String.Empty : "custom ")}BYOND version {model.Version.Major}.{model.Version.Minor}",
							StartedBy = AuthenticationContext.User,
							CancelRightsType = RightsType.Byond,
							CancelRight = (ulong)ByondRights.CancelInstall,
							Instance = Instance
						};
						await jobManager.RegisterOperation(
							job,
							(core, databaseContextFactory, paramJob, progressHandler, jobCancellationToken) => core.ByondManager.ChangeVersion(
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
				})
				.ConfigureAwait(false);
		}
	}
}
