using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// The <see cref="ApiController"/> for <see cref="ConfigurationFile"/>s
	/// </summary>
	[Route(Routes.Configuration)]
	public sealed class ConfigurationController : ApiController
	{
		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="ConfigurationController"/>
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="ConfigurationController"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// Construct a <see cref="UserController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		public ConfigurationController(
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			IInstanceManager instanceManager,
			IIOManager ioManager,
			ILogger<ConfigurationController> logger)
			: base(
				  databaseContext,
				  authenticationContextFactory,
				  logger,
				  true)
		{
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
		}

		/// <summary>
		/// If a <see cref="ForbidResult"/> should be returned from actions due to conflicts with one or both of the <see cref="Api.Models.Instance.ConfigurationType"/> or the <see cref="IAuthenticationContext.SystemIdentity"/> or a given <paramref name="path"/> tries to access parent directories
		/// </summary>
		/// <param name="path">The path to validate if any</param>
		/// <param name="systemIdentityToUse">The <see cref="ISystemIdentity"/> to use when calling into <see cref="Components.StaticFiles.IConfiguration"/></param>
		/// <returns><see langword="true"/> if a <see cref="ForbidResult"/> should be returned, <see langword="false"/> otherwise</returns>
		bool ForbidDueToModeConflicts(string path, out ISystemIdentity systemIdentityToUse)
		{
			if (Instance.ConfigurationType == ConfigurationType.Disallowed
				|| (Instance.ConfigurationType == ConfigurationType.SystemIdentityWrite && AuthenticationContext.SystemIdentity == null)
				|| (path != null && ioManager.PathContainsParentAccess(path)))
			{
				systemIdentityToUse = null;
				return true;
			}

			systemIdentityToUse = Instance.ConfigurationType == ConfigurationType.SystemIdentityWrite ? AuthenticationContext.SystemIdentity : null;
			return false;
		}

		/// <summary>
		/// Write to a configuration file.
		/// </summary>
		/// <param name="model">The <see cref="ConfigurationFile"/> representing the file.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">File updated successfully.</response>
		/// <response code="201">File created successfully.</response>
		[HttpPost]
		[TgsAuthorize(ConfigurationRights.Write)]
		[ProducesResponseType(typeof(ConfigurationFile), 200)]
		[ProducesResponseType(typeof(ConfigurationFile), 201)]
		public async Task<IActionResult> Update([FromBody] ConfigurationFile model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			if (ForbidDueToModeConflicts(model.Path, out var systemIdentity))
				return Forbid();

			var config = instanceManager.GetInstance(Instance).Configuration;
			try
			{
				var newFile = await config.Write(model.Path, systemIdentity, model.Content, model.LastReadHash, cancellationToken).ConfigureAwait(false);
				if (newFile == null)
					return Conflict(new ErrorMessage(ErrorCode.ConfigurationFileUpdated));

				newFile.Content = null;

				return model.LastReadHash == null ? (IActionResult)Created(newFile) : Json(newFile);
			}
			catch(IOException e)
			{
				Logger.LogInformation("IOException while updating file {0}: {1}", model.Path, e);
				return Conflict(new ErrorMessage(ErrorCode.IOError)
				{
					AdditionalData = e.Message
				});
			}
			catch (NotImplementedException)
			{
				return RequiresPosixSystemIdentity();
			}
		}

		/// <summary>
		/// Get the contents of a file at a <paramref name="filePath"/>
		/// </summary>
		/// <param name="filePath">The path of the file to get</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation</returns>>
		/// <response code="200">File read successfully.</response>>
		/// <response code="410">File does not currently exist.</response>
		[HttpGet(Routes.File + "/{*filePath}")]
		[TgsAuthorize(ConfigurationRights.Read)]
		[ProducesResponseType(typeof(ConfigurationFile), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 410)]
		public async Task<IActionResult> File(string filePath, CancellationToken cancellationToken)
		{
			if (ForbidDueToModeConflicts(filePath, out var systemIdentity))
				return Forbid();

			try
			{
				var result = await instanceManager.GetInstance(Instance).Configuration.Read(filePath, systemIdentity, cancellationToken).ConfigureAwait(false);
				if (result == null)
					return Gone();

				return Json(result);
			}
			catch (IOException e)
			{
				Logger.LogInformation("IOException while reading file {0}: {1}", filePath, e);
				return Conflict(new ErrorMessage(ErrorCode.IOError)
				{
					AdditionalData = e.Message
				});
			}
			catch (NotImplementedException)
			{
				return RequiresPosixSystemIdentity();
			}
		}

		/// <summary>
		/// Get the contents of a directory at a <paramref name="directoryPath"/>
		/// </summary>
		/// <param name="directoryPath">The path of the directory to get</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation</returns>
		/// <response code="200">Directory listed successfully.</response>>
		/// <response code="410">Directory does not currently exist.</response>
		[HttpGet(Routes.List + "/{*directoryPath}")]
		[TgsAuthorize(ConfigurationRights.List)]
		[ProducesResponseType(typeof(IReadOnlyList<ConfigurationFile>), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 410)]
		public async Task<IActionResult> Directory(string directoryPath, CancellationToken cancellationToken)
		{
			if (ForbidDueToModeConflicts(directoryPath, out var systemIdentity))
				return Forbid();

			try
			{
				var result = await instanceManager.GetInstance(Instance).Configuration.ListDirectory(directoryPath, systemIdentity, cancellationToken).ConfigureAwait(false);
				if (result == null)
					return Gone();

				return Json(result);
			}
			catch (NotImplementedException)
			{
				return RequiresPosixSystemIdentity();
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}

		/// <summary>
		/// Get the contents of the root configuration directory.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		[HttpGet(Routes.List)]
		[TgsAuthorize(ConfigurationRights.List)]
		[ProducesResponseType(typeof(IReadOnlyList<ConfigurationFile>), 200)]
		public Task<IActionResult> List(CancellationToken cancellationToken) => Directory(null, cancellationToken);

		/// <summary>
		/// Create a configuration directory.
		/// </summary>
		/// <param name="model">The <see cref="ConfigurationFile"/> representing the directory.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Directory already exists.</response>
		/// <response code="201">Directory created successfully.</response>
		[HttpPut]
		[TgsAuthorize(ConfigurationRights.Write)]
		[ProducesResponseType(typeof(ConfigurationFile), 200)]
		[ProducesResponseType(typeof(ConfigurationFile), 201)]
		public async Task<IActionResult> Create([FromBody] ConfigurationFile model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			if (ForbidDueToModeConflicts(model.Path, out var systemIdentity))
				return Forbid();

			try
			{
				model.IsDirectory = true;
				return await instanceManager
					.GetInstance(Instance)
					.Configuration
					.CreateDirectory(model.Path, systemIdentity, cancellationToken)
					.ConfigureAwait(false)
					? (IActionResult)Json(model)
					: Created(model);
			}
			catch (IOException e)
			{
				Logger.LogInformation("IOException while creating directory {0}: {1}", model.Path, e);
				return Conflict(new ErrorMessage(ErrorCode.IOError)
				{
					Message = e.Message
				});
			}
			catch (NotImplementedException)
			{
				return RequiresPosixSystemIdentity();
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}

		/// <summary>
		/// Deletes an empty <paramref name="directory"/>
		/// </summary>
		/// <param name="directory">A <see cref="ConfigurationFile"/> representing the path to the directory to delete</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation</returns>
		/// <response code="204">Empty directory deleted successfully.</response>
		[HttpDelete]
		[TgsAuthorize(ConfigurationRights.Delete)]
		[ProducesResponseType(204)]
		public async Task<IActionResult> Delete([FromBody] ConfigurationFile directory, CancellationToken cancellationToken)
		{
			if (directory == null)
				throw new ArgumentNullException(nameof(directory));

			if (ForbidDueToModeConflicts(directory.Path, out var systemIdentity))
				return Forbid();

			try
			{
				return await instanceManager
					.GetInstance(Instance)
					.Configuration
					.DeleteDirectory(directory.Path, systemIdentity, cancellationToken)
					.ConfigureAwait(false)
					? (IActionResult)NoContent()
					: Conflict(new ErrorMessage(ErrorCode.ConfigurationDirectoryNotEmpty));
			}
			catch (NotImplementedException)
			{
				return RequiresPosixSystemIdentity();
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
		}
	}
}
