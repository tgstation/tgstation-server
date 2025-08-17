using System;
using System.IO;
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
using Tgstation.Server.Host.Controllers.Results;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils;

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
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/> for the <see cref="InstanceRequiredController"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="apiHeaders">The <see cref="IApiHeadersProvider"/> for the <see cref="InstanceRequiredController"/>.</param>
		public ConfigurationController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			ILogger<ConfigurationController> logger,
			IInstanceManager instanceManager,
			IIOManager ioManager,
			IApiHeadersProvider apiHeaders)
			: base(
				  databaseContext,
				  authenticationContext,
				  logger,
				  instanceManager,
				  apiHeaders)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
		}

		/// <summary>
		/// Write to a configuration file.
		/// </summary>
		/// <param name="model">The <see cref="ConfigurationFileRequest"/> representing the file.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">File updated successfully.</response>
		/// <response code="202">File upload ticket created successfully.</response>
		[HttpPost]
		[TgsAuthorize(ConfigurationRights.Write)]
		[ProducesResponseType(typeof(ConfigurationFileResponse), 200)]
		[ProducesResponseType(typeof(ConfigurationFileResponse), 202)]
		public async ValueTask<IActionResult> Update([FromBody] ConfigurationFileRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);
			if (ForbidDueToModeConflicts(model.Path!, out var systemIdentity))
				return Forbid();

			try
			{
				return await WithComponentInstance(
					async instance =>
					{
						var newFile = await instance
							.Configuration
							.Write(
								model.Path!,
								systemIdentity,
								model.LastReadHash,
								cancellationToken);

						if (newFile == null)
							return Conflict(new ErrorMessageResponse(ErrorCode.ConfigurationContendedAccess));

						return model.LastReadHash == null ? Accepted(newFile) : Json(newFile);
					});
			}
			catch (IOException e)
			{
				Logger.LogInformation(e, "IOException while updating file {path}!", model.Path);
				return Conflict(new ErrorMessageResponse(ErrorCode.IOError)
				{
					AdditionalData = e.Message,
				});
			}
		}

		/// <summary>
		/// Get the contents of a file at a <paramref name="filePath"/>.
		/// </summary>
		/// <param name="filePath">The path of the file to get.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>>
		/// <response code="200">File read successfully.</response>>
		/// <response code="410">File does not currently exist.</response>
		[HttpGet(Routes.File + "/{*filePath}")]
		[TgsAuthorize(ConfigurationRights.Read)]
		[ProducesResponseType(typeof(ConfigurationFileResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 409)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public async ValueTask<IActionResult> File(string filePath, CancellationToken cancellationToken)
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
							.Read(filePath, systemIdentity, cancellationToken);

						if (result == null)
							return Conflict(new ErrorMessageResponse(ErrorCode.ConfigurationContendedAccess));

						return Json(result);
					});
			}
			catch (IOException e)
			{
				Logger.LogInformation(e, "IOException while reading file {path}!", filePath);
				return Conflict(new ErrorMessageResponse(ErrorCode.IOError)
				{
					AdditionalData = e.Message,
				});
			}
		}

		/// <summary>
		/// Get the contents of a directory at a <paramref name="directoryPath"/>.
		/// </summary>
		/// <param name="directoryPath">The path of the directory to get.</param>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Directory listed successfully.</response>>
		/// <response code="410">Directory does not currently exist.</response>
		[HttpGet(Routes.List + "/{*directoryPath}")]
		[TgsAuthorize(ConfigurationRights.List)]
		[ProducesResponseType(typeof(PaginatedResponse<ConfigurationFileResponse>), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 409)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		public ValueTask<IActionResult> Directory(
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
								.ListDirectory(directoryPath, systemIdentity, cancellationToken);

							if (result == null)
								return new PaginatableResult<ConfigurationFileResponse>(
									Conflict(new ErrorMessageResponse(ErrorCode.ConfigurationContendedAccess)));

							return new PaginatableResult<ConfigurationFileResponse>(result);
						}
						catch (UnauthorizedAccessException)
						{
							return new PaginatableResult<ConfigurationFileResponse>(
								Forbid());
						}
						catch (IOException ex)
						{
							Logger.LogInformation(ex, "IOException while enumerating directory!");
							return new PaginatableResult<ConfigurationFileResponse>(
								Conflict(new ErrorMessageResponse(ErrorCode.IOError)
								{
									AdditionalData = ex.Message,
								}));
						}
					},
					page,
					pageSize,
					cancellationToken));

		/// <summary>
		/// Get the contents of the root configuration directory.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		[HttpGet(Routes.List)]
		[TgsAuthorize(ConfigurationRights.List)]
		[ProducesResponseType(typeof(PaginatedResponse<ConfigurationFileResponse>), 200)]
		public ValueTask<IActionResult> List(
			[FromQuery] int? page,
			[FromQuery] int? pageSize,
			CancellationToken cancellationToken) => Directory(null, page, pageSize, cancellationToken);

		/// <summary>
		/// Create a configuration directory.
		/// </summary>
		/// <param name="model">The <see cref="ConfigurationFileRequest"/> representing the directory.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Directory already exists.</response>
		/// <response code="201">Directory created successfully.</response>
		[HttpPut]
		[TgsAuthorize(ConfigurationRights.Write)]
		[ProducesResponseType(typeof(ConfigurationFileResponse), 200)]
		[ProducesResponseType(typeof(ConfigurationFileResponse), 201)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 409)]
		public async ValueTask<IActionResult> CreateDirectory([FromBody] ConfigurationFileRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);

			if (model.Path == null)
				return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));

			if (ForbidDueToModeConflicts(model.Path, out var systemIdentity))
				return Forbid();

			try
			{
				var resultModel = new ConfigurationFileResponse
				{
					IsDirectory = true,
					Path = model.Path,
				};

				return await WithComponentInstance(
					async instance =>
					{
						var result = await instance
							.Configuration
							.CreateDirectory(model.Path, systemIdentity, cancellationToken);

						if (!result.HasValue)
							return Conflict(new ErrorMessageResponse(ErrorCode.ConfigurationContendedAccess));

						return result.Value
							? Json(resultModel)
							: this.Created(resultModel);
					});
			}
			catch (IOException e)
			{
				Logger.LogInformation(e, "IOException while creating directory {path}!", model.Path);
				return Conflict(new ErrorMessageResponse(ErrorCode.IOError)
				{
					Message = e.Message,
				});
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
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		/// <response code="204">Empty directory deleted successfully.</response>
		[HttpDelete]
		[TgsAuthorize(ConfigurationRights.Delete)]
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 409)]
		public async ValueTask<IActionResult> DeleteDirectory([FromBody] ConfigurationFileRequest directory, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(directory);

			if (directory.Path == null)
				return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure));

			if (ForbidDueToModeConflicts(directory.Path, out var systemIdentity))
				return Forbid();

			try
			{
				return await WithComponentInstance(
					async instance =>
					{
						var result = await instance
							.Configuration
							.DeleteDirectory(directory.Path, systemIdentity, cancellationToken);

						if (!result.HasValue)
							return Conflict(new ErrorMessageResponse(ErrorCode.ConfigurationContendedAccess));

						return result.Value
							? NoContent()
							: Conflict(new ErrorMessageResponse(ErrorCode.ConfigurationDirectoryNotEmpty));
					});
			}
			catch (UnauthorizedAccessException)
			{
				return Forbid();
			}
			catch (IOException ex)
			{
				Logger.LogInformation(ex, "IOException while deleting directory!");
				return Conflict(new ErrorMessageResponse(ErrorCode.IOError)
				{
					Message = ex.Message,
				});
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
				|| (path != null && ioManager.PathContainsParentAccess(path)))
			{
				systemIdentityToUse = null;
				return true;
			}

			systemIdentityToUse = Instance.ConfigurationType == ConfigurationType.SystemIdentityWrite ? AuthenticationContext.SystemIdentity : null;
			return false;
		}
	}
}
