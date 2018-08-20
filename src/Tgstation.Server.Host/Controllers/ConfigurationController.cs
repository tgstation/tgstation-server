using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
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
	[Route(Routes.Configuration)]
	public sealed class ConfigurationController : ModelController<ConfigurationFile>
	{
		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="ConfigurationController"/>
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// Construct a <see cref="UserController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		public ConfigurationController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, IInstanceManager instanceManager, ILogger<ConfigurationController> logger) : base(databaseContext, authenticationContextFactory, logger, true)
		{
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

				return model.LastReadHash == null ? (IActionResult)StatusCode((int)HttpStatusCode.Created, newFile) : Json(newFile);
			}
			catch(NotImplementedException)
			{
				return StatusCode((int)HttpStatusCode.NotImplemented);
			}
		}

		/// <summary>
		/// Get the contents of a file at a <paramref name="filePath"/>
		/// </summary>
		/// <param name="filePath">The path of the file to get</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation</returns>
		[HttpGet("File/{*filePath}")]
		[TgsAuthorize(ConfigurationRights.Read)]
		public async Task<IActionResult> File(string filePath, CancellationToken cancellationToken)
		{
			if (ForbidDueToModeConflicts())
				return Forbid();

			try
			{
				var result = await instanceManager.GetInstance(Instance).Configuration.Read(filePath, AuthenticationContext.SystemIdentity, cancellationToken).ConfigureAwait(false);
				if (result == null)
					return StatusCode((int)HttpStatusCode.Gone);

				return Json(result);
			}
			catch (NotImplementedException)
			{
				return StatusCode((int)HttpStatusCode.NotImplemented);
			}
		}

		/// <summary>
		/// Get the contents of a directory at a <paramref name="directoryPath"/>
		/// </summary>
		/// <param name="directoryPath">The path of the directory to get</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation</returns>
		[HttpGet("List/{*directoryPath}")]
		[TgsAuthorize(ConfigurationRights.List)]
		public async Task<IActionResult> Directory(string directoryPath, CancellationToken cancellationToken)
		{
			if (ForbidDueToModeConflicts())
				return Forbid();

			try
			{
				var result = await instanceManager.GetInstance(Instance).Configuration.ListDirectory(directoryPath, AuthenticationContext.SystemIdentity, cancellationToken).ConfigureAwait(false);
				if (result == null)
					return StatusCode((int)HttpStatusCode.Gone);

				return Json(result);
			}
			catch (NotImplementedException)
			{
				return StatusCode((int)HttpStatusCode.NotImplemented);
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}

		/// <inheritdoc />
		[TgsAuthorize(ConfigurationRights.List)]
		public override Task<IActionResult> List(CancellationToken cancellationToken) => Directory(null, cancellationToken);

		/// <inheritdoc />
		[TgsAuthorize(ConfigurationRights.Write)]
		public override async Task<IActionResult> Create([FromBody] ConfigurationFile model, CancellationToken cancellationToken)
		{
			if (ForbidDueToModeConflicts())
				return Forbid();

			try
			{
				return await instanceManager.GetInstance(Instance).Configuration.CreateDirectory(model.Path, AuthenticationContext.SystemIdentity, cancellationToken).ConfigureAwait(false) ? (IActionResult)Json(model) : StatusCode((int)HttpStatusCode.Created, model);
			}
			catch (NotImplementedException)
			{
				return StatusCode((int)HttpStatusCode.NotImplemented);
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}
	}
}
