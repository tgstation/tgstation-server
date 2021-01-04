using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
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
	public sealed class ConfigurationController : InstanceRequiredController
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="ConfigurationController"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// Construct a <see cref="UserController"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/></param>
		public ConfigurationController(
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			IInstanceManager instanceManager,
			IIOManager ioManager,
			ILogger<ConfigurationController> logger)
			: base(
				  instanceManager,
				  databaseContext,
				  authenticationContextFactory,
				  logger)
		{
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
		/// <response code="202">File upload ticket created successfully.</response>
		[HttpPost]
		[TgsAuthorize(ConfigurationRights.Write)]
		[ProducesResponseType(typeof(ConfigurationFile), 200)]
		[ProducesResponseType(typeof(ConfigurationFile), 202)]
		public async Task<IActionResult> Update([FromBody] ConfigurationFile model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			if (ForbidDueToModeConflicts(model.Path, out var systemIdentity))
				return Forbid();

			try
			{
				return await WithComponentInstance(
					async instance =>
					{
						var newFile = await instance
							.Configuration
							.Write(
								model.Path,
								systemIdentity,
								model.LastReadHash,
								cancellationToken)
							.ConfigureAwait(false);

						return model.LastReadHash == null ? (IActionResult)Accepted(newFile) : Json(newFile);
					})
					.ConfigureAwait(false);
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
				return await WithComponentInstance(
					async instance =>
					{
						var result = await instance
							.Configuration
							.Read(filePath, systemIdentity, cancellationToken)
							.ConfigureAwait(false);
						if (result == null)
							return Gone();

						return Json(result);
					})
					.ConfigureAwait(false);
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
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation</returns>
		/// <response code="200">Directory listed successfully.</response>>
		/// <response code="410">Directory does not currently exist.</response>
		[HttpGet(Routes.List + "/{*directoryPath}")]
		[TgsAuthorize(ConfigurationRights.List)]
		[ProducesResponseType(typeof(Paginated<ConfigurationFile>), 200)]
		[ProducesResponseType(typeof(ErrorMessage), 410)]
		public Task<IActionResult> Directory(
			string directoryPath,
			[FromQuery] int? page,
			[FromQuery] int? pageSize,
			CancellationToken cancellationToken)
			=> WithComponentInstance(
				instance => Paginated(
					async () =>
					{
						if (ForbidDueToModeConflicts(directoryPath, out var systemIdentity))
							return new PaginatableResult<ConfigurationFile>(
								Forbid());

						try
						{
							var result = await instance
								.Configuration
								.ListDirectory(directoryPath, systemIdentity, cancellationToken)
								.ConfigureAwait(false);
							if (result == null)
								return new PaginatableResult<ConfigurationFile>(Gone());

							return new PaginatableResult<ConfigurationFile>(
								result
									.AsQueryable()
									.OrderBy(x => x.Path));
						}
						catch (NotImplementedException)
						{
							return new PaginatableResult<ConfigurationFile>(
								RequiresPosixSystemIdentity());
						}
						catch (UnauthorizedAccessException)
						{
							return new PaginatableResult<ConfigurationFile>(
								Forbid());
						}
					},
					null,
					page,
					pageSize,
					cancellationToken));

		/// <summary>
		/// Get the contents of the root configuration directory.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		[HttpGet(Routes.List)]
		[TgsAuthorize(ConfigurationRights.List)]
		[ProducesResponseType(typeof(Paginated<ConfigurationFile>), 200)]
		public Task<IActionResult> List(
			[FromQuery] int? page,
			[FromQuery] int? pageSize,
			CancellationToken cancellationToken) => Directory(null, page, pageSize, cancellationToken);

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
				return await WithComponentInstance(
					async instance => await instance
					.Configuration
					.CreateDirectory(model.Path, systemIdentity, cancellationToken)
					.ConfigureAwait(false)
					? (IActionResult)Json(model)
					: Created(model))
					.ConfigureAwait(false);
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

			if (directory.Path == null)
				return BadRequest(new ErrorMessage(ErrorCode.ModelValidationFailure));

			if (ForbidDueToModeConflicts(directory.Path, out var systemIdentity))
				return Forbid();

			try
			{
				return await WithComponentInstance(
					async instance => await instance
					.Configuration
					.DeleteDirectory(directory.Path, systemIdentity, cancellationToken)
					.ConfigureAwait(false)
					? (IActionResult)NoContent()
					: Conflict(new ErrorMessage(ErrorCode.ConfigurationDirectoryNotEmpty)))
					.ConfigureAwait(false);
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
