using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// The <see cref="ApiController"/> for <see cref="IConfigurationFile"/>s.
	/// </summary>
	[Route(Routes.Configuration)]
	public sealed class ConfigurationController : InstanceRequiredController
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="ConfigurationController"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigurationController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/>.</param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/>.</param>
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
		/// Write to a configuration file.
		/// </summary>
		/// <param name="model">The <see cref="ConfigurationFileRequest"/> representing the file.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">File updated successfully.</response>
		/// <response code="202">File upload ticket created successfully.</response>
		[HttpPost]
		[TgsAuthorize(ConfigurationRights.Write)]
		[ProducesResponseType(typeof(ConfigurationFileResponse), 200)]
		[ProducesResponseType(typeof(ConfigurationFileResponse), 202)]
		public async Task<IActionResult> Update([FromBody] ConfigurationFileRequest model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			var path = model.Path!;
			if (ForbidDueToModeConflicts(path, out var systemIdentity))
				return Forbid();

			try
			{
				return await WithComponentInstance(
					async instance =>
					{
						var newFile = await instance
							.Configuration
							.Write(
								path,
								systemIdentity,
								model.LastReadHash,
								cancellationToken)
							.ConfigureAwait(false);

						return model.LastReadHash == null ? Accepted(newFile) : Json(newFile);
					})
					.ConfigureAwait(false);
			}
			catch (IOException e)
			{
				Logger.LogInformation("IOException while updating file {0}: {1}", model.Path, e);
				return Conflict(new ErrorMessageResponse(ErrorCode.IOError)
				{
					AdditionalData = e.Message,
				});
			}
			catch (NotImplementedException)
			{
				return RequiresPosixSystemIdentity();
			}
		}

		/// <summary>
		/// Get the contents of a file at a <paramref name="filePath"/>.
		/// </summary>
		/// <param name="filePath">The path of the file to get.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>>
		/// <response code="200">File read successfully.</response>>
		/// <response code="410">File does not currently exist.</response>
		[HttpGet(Routes.File + "/{*filePath}")]
		[TgsAuthorize(ConfigurationRights.Read)]
		[ProducesResponseType(typeof(ConfigurationFileResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
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
				return Conflict(new ErrorMessageResponse(ErrorCode.IOError)
				{
					AdditionalData = e.Message,
				});
			}
			catch (NotImplementedException)
			{
				return RequiresPosixSystemIdentity();
			}
		}

		/// <summary>
		/// Get the contents of a directory at a <paramref name="directoryPath"/>.
		/// </summary>
		/// <param name="directoryPath">The path of the directory to get.</param>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Directory listed successfully.</response>>
		/// <response code="410">Directory does not currently exist.</response>
		[HttpGet(Routes.List + "/{*directoryPath}")]
		[TgsAuthorize(ConfigurationRights.List)]
		[ProducesResponseType(typeof(PaginatedResponse<ConfigurationFileResponse>), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public Task<IActionResult> Directory(
			string? directoryPath,
			[FromQuery] int? page,
			[FromQuery] int? pageSize,
			CancellationToken cancellationToken)
			=> WithComponentInstance(
				instance => Paginated(
					async () =>
					{
						if (ForbidDueToModeConflicts(directoryPath, out var systemIdentity))
							return new PaginatableResult<ConfigurationFileResponse>(
								Forbid());

						try
						{
							var result = await instance
								.Configuration
								.ListDirectory(directoryPath, systemIdentity, cancellationToken)
								.ConfigureAwait(false);
							if (result == null)
								return new PaginatableResult<ConfigurationFileResponse>(Gone());

							return new PaginatableResult<ConfigurationFileResponse>(
								result
									.AsQueryable()
									.OrderBy(x => x.Path));
						}
						catch (NotImplementedException)
						{
							return new PaginatableResult<ConfigurationFileResponse>(
								RequiresPosixSystemIdentity());
						}
						catch (UnauthorizedAccessException)
						{
							return new PaginatableResult<ConfigurationFileResponse>(
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
		[ProducesResponseType(typeof(PaginatedResponse<ConfigurationFileResponse>), 200)]
		public Task<IActionResult> List(
			[FromQuery] int? page,
			[FromQuery] int? pageSize,
			CancellationToken cancellationToken) => Directory(null, page, pageSize, cancellationToken);

		/// <summary>
		/// Create a configuration directory.
		/// </summary>
		/// <param name="model">The <see cref="ConfigurationFileRequest"/> representing the directory.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Directory already exists.</response>
		/// <response code="201">Directory created successfully.</response>
		[HttpPut]
		[TgsAuthorize(ConfigurationRights.Write)]
		[ProducesResponseType(typeof(ConfigurationFileResponse), 200)]
		[ProducesResponseType(typeof(ConfigurationFileResponse), 201)]
		public async Task<IActionResult> CreateDirectory([FromBody] ConfigurationFileRequest model, CancellationToken cancellationToken)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			var path = model.Path!;
			if (ForbidDueToModeConflicts(path, out var systemIdentity))
				return Forbid();

			try
			{
				var resultModel = new ConfigurationFileResponse
				{
					IsDirectory = true,
					Path = path,
				};

				return await WithComponentInstance(
					async instance => await instance
						.Configuration
						.CreateDirectory(path, systemIdentity, cancellationToken)
						.ConfigureAwait(false)
						? Json(resultModel)
						: Created(resultModel))
					.ConfigureAwait(false);
			}
			catch (IOException e)
			{
				Logger.LogInformation("IOException while creating directory {0}: {1}", model.Path, e);
				return Conflict(new ErrorMessageResponse(ErrorCode.IOError)
				{
					Message = e.Message,
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
		/// Deletes an empty <paramref name="directory"/>.
		/// </summary>
		/// <param name="directory">A <see cref="ConfigurationFileRequest"/> representing the path to the directory to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="204">Empty directory deleted successfully.</response>
		[HttpDelete]
		[TgsAuthorize(ConfigurationRights.Delete)]
		[ProducesResponseType(204)]
		public async Task<IActionResult> DeleteDirectory([FromBody] ConfigurationFileRequest directory, CancellationToken cancellationToken)
		{
			if (directory == null)
				throw new ArgumentNullException(nameof(directory));

			if (directory.Path == null)
				return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));

			if (ForbidDueToModeConflicts(directory.Path, out var systemIdentity))
				return Forbid();

			try
			{
				return await WithComponentInstance(
					async instance => await instance
					.Configuration
					.DeleteDirectory(directory.Path, systemIdentity, cancellationToken)
					.ConfigureAwait(false)
					? NoContent()
					: Conflict(new ErrorMessageResponse(ErrorCode.ConfigurationDirectoryNotEmpty)))
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

		/// <summary>
		/// If a <see cref="ForbidResult"/> should be returned from actions due to conflicts with one or both of the <see cref="Api.Models.Instance.ConfigurationType"/> or the <see cref="IAuthenticationContext.SystemIdentity"/> or a given <paramref name="path"/> tries to access parent directories.
		/// </summary>
		/// <param name="path">The path to validate if any.</param>
		/// <param name="systemIdentityToUse">The <see cref="ISystemIdentity"/> to use when calling into <see cref="Components.StaticFiles.IConfiguration"/>.</param>
		/// <returns><see langword="true"/> if a <see cref="ForbidResult"/> should be returned, <see langword="false"/> otherwise.</returns>
		bool ForbidDueToModeConflicts(string? path, out ISystemIdentity? systemIdentityToUse)
		{
			if (Instance.ConfigurationType == ConfigurationType.Disallowed
				|| (Instance.ConfigurationType == ConfigurationType.SystemIdentityWrite && AuthenticationContext.SystemIdentity == null)
				|| ((path != null) && ioManager.PathContainsParentAccess(path)))
			{
				systemIdentityToUse = null;
				return true;
			}

			systemIdentityToUse = Instance.ConfigurationType == ConfigurationType.SystemIdentityWrite ? AuthenticationContext.SystemIdentity : null;
			return false;
		}
	}
}
