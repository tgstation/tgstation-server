using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// The <see cref="ModelController{TModel}"/> for <see cref="ConfigurationFile"/>s
	/// </summary>
	[Route("/" + nameof(Configuration))]
	public sealed class ConfigurationController : ModelController<ConfigurationFile>
	{
		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="ConfigurationController"/>
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ConfigurationController"/>
		/// </summary>
		readonly ILogger<ConfigurationController> logger;

		/// <summary>
		/// Construct a <see cref="UserController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public ConfigurationController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IInstanceManager instanceManager, ILogger<ConfigurationController> logger) : base(databaseContext, authenticationContextFactory, true)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
		}

		/// <summary>
		/// If a <see cref="ForbidResult"/> should be returned from actions due to conflicts with one or both of the <see cref="Api.Models.Instance.ConfigurationType"/> or the <see cref="IAuthenticationContext.SystemIdentity"/>
		/// </summary>
		/// <returns><see langword="true"/> if a <see cref="ForbidResult"/> should be returned, <see langword="false"/> otherwise</returns>
		bool ForbidDueToModeConflicts() => Instance.ConfigurationType == ConfigurationType.Disallowed || (Instance.ConfigurationType == ConfigurationType.SystemIdentityWrite && AuthenticationContext.SystemIdentity == null);

		/// <inheritdoc />
		[TgsAuthorize(ConfigurationRights.Write)]
		public override async Task<IActionResult> Update([FromBody] ConfigurationFile model, CancellationToken cancellationToken)
		{
			if (ForbidDueToModeConflicts())
				return Forbid();

			var config = instanceManager.GetInstance(Instance).Configuration;
			try
			{
				var newFile = await config.Write(model.Path, AuthenticationContext.SystemIdentity, model.Content, model.LastReadHash, cancellationToken).ConfigureAwait(false);
				if (newFile == null)
					return Conflict();

				newFile.Content = null;

				return Json(newFile);
			}
			catch(NotImplementedException e)
			{
				return StatusCode((int)HttpStatusCode.NotImplemented, new { message = e.Message });
			}
		}

		/// <summary>
		/// Get the contents of a file at a <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path of the file to get</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation</returns>
		[HttpGet("/File/{path}")]
		[TgsAuthorize(ConfigurationRights.Read)]
		public async Task<IActionResult> File(string path, CancellationToken cancellationToken)
		{
			if (ForbidDueToModeConflicts())
				return Forbid();

			try
			{
				var result = await instanceManager.GetInstance(Instance).Configuration.Read(path, AuthenticationContext.SystemIdentity, cancellationToken).ConfigureAwait(false);
				if (result == null || result.IsDirectory.Value)
					return NotFound();

				return Json(result);
			}
			catch (NotImplementedException e)
			{
				return StatusCode((int)HttpStatusCode.NotImplemented, new { message = e.Message });
			}
		}

		/// <summary>
		/// Get the contents of a directory at a <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path of the directory to get</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation</returns>
		[HttpGet("/List/{path}")]
		[TgsAuthorize(ConfigurationRights.List)]
		public async Task<IActionResult> Directory(string path, CancellationToken cancellationToken)
		{
			if (ForbidDueToModeConflicts())
				return Forbid();

			try
			{
				var result = await instanceManager.GetInstance(Instance).Configuration.ListDirectory(path, AuthenticationContext.SystemIdentity, cancellationToken).ConfigureAwait(false);
				if (result == null)
					return NotFound();

				return Json(result);
			}
			catch (NotImplementedException e)
			{
				return StatusCode((int)HttpStatusCode.NotImplemented, new { message = e.Message });
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}
	}
}
