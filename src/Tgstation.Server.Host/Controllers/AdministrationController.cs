using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers.Results;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Transfer;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="ApiController"/> for TGS administration purposes.
	/// </summary>
	[Authorize]
	[Route(Routes.Administration)]
	public sealed class AdministrationController : ApiController
	{
		/// <summary>
		/// The <see cref="IRestAuthorityInvoker{TAuthority}"/> for the <see cref="IAdministrationAuthority"/>.
		/// </summary>
		readonly IRestAuthorityInvoker<IAdministrationAuthority> administrationAuthority;

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="AdministrationController"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="AdministrationController"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="AdministrationController"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="IFileTransferTicketProvider"/> for the <see cref="AdministrationController"/>.
		/// </summary>
		readonly IFileTransferTicketProvider fileTransferService;

		/// <summary>
		/// The <see cref="FileLoggingConfiguration"/> for the <see cref="AdministrationController"/>.
		/// </summary>
		readonly FileLoggingConfiguration fileLoggingConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="AdministrationController"/> class.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="apiHeadersProvider">The <see cref="IApiHeadersProvider"/> for the <see cref="ApiController"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ApiController"/>.</param>
		/// <param name="administrationAuthority">The value of <see cref="administrationAuthority"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="fileTransferService">The value of <see cref="fileTransferService"/>.</param>
		/// <param name="fileLoggingConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="fileLoggingConfiguration"/>.</param>
		public AdministrationController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			IApiHeadersProvider apiHeadersProvider,
			ILogger<AdministrationController> logger,
			IRestAuthorityInvoker<IAdministrationAuthority> administrationAuthority,
			IAssemblyInformationProvider assemblyInformationProvider,
			IIOManager ioManager,
			IPlatformIdentifier platformIdentifier,
			IFileTransferTicketProvider fileTransferService,
			IOptions<FileLoggingConfiguration> fileLoggingConfigurationOptions)
			: base(
				  databaseContext,
				  authenticationContext,
				  apiHeadersProvider,
				  logger,
				  true)
		{
			this.administrationAuthority = administrationAuthority ?? throw new ArgumentNullException(nameof(administrationAuthority));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.fileTransferService = fileTransferService ?? throw new ArgumentNullException(nameof(fileTransferService));
			fileLoggingConfiguration = fileLoggingConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(fileLoggingConfigurationOptions));
		}

		/// <summary>
		/// Get <see cref="AdministrationResponse"/> server information.
		/// </summary>
		/// <param name="fresh">If <see langword="true"/>, the cache should be bypassed.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="200">Retrieved <see cref="AdministrationResponse"/> data successfully.</response>
		/// <response code="424">The GitHub API rate limit was hit. See response header Retry-After.</response>
		/// <response code="429">A GitHub API error occurred. See error message for details.</response>
		[HttpGet]
		[ProducesResponseType(typeof(AdministrationResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 424)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 429)]
		public ValueTask<IActionResult> Read([FromQuery] bool? fresh, CancellationToken cancellationToken)
			=> administrationAuthority.Invoke<AdministrationResponse, AdministrationResponse>(
				this,
				authority => authority.GetUpdateInformation(fresh ?? false, cancellationToken));

		/// <summary>
		/// Attempt to perform a server upgrade.
		/// </summary>
		/// <param name="model">The <see cref="ServerUpdateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		/// <response code="202">Update has been started successfully.</response>
		/// <response code="410">The requested release version could not be found in the target GitHub repository.</response>
		/// <response code="422">Upgrade operations are unavailable due to the launch configuration of TGS.</response>
		/// <response code="424">A GitHub rate limit was encountered or the swarm integrity check failed.</response>
		/// <response code="429">A GitHub API error occurred.</response>
		[HttpPost]
		[ProducesResponseType(typeof(ServerUpdateResponse), 202)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 410)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 422)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 424)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 429)]
		public async ValueTask<IActionResult> Update([FromBody] ServerUpdateRequest model, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(model);

			if (model.NewVersion == null)
				return BadRequest(new ErrorMessageResponse(ErrorCode.ModelValidationFailure)
				{
					AdditionalData = "newVersion is required!",
				});

			return await administrationAuthority.Invoke<ServerUpdateResponse, ServerUpdateResponse>(
				this,
				authority => authority.TriggerServerVersionChange(model.NewVersion, model.UploadZip ?? false, cancellationToken));
		}

		/// <summary>
		/// Attempts to restart the server.
		/// </summary>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="204">Restart begun successfully.</response>
		/// <response code="422">Restart operations are unavailable due to the launch configuration of TGS.</response>
		[HttpDelete]
		[HttpPost(Routes.Restart)]
		[ProducesResponseType(204)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 422)]
		public ValueTask<IActionResult> Delete()
#pragma warning disable API1001 // Action returns undeclared success result
			=> administrationAuthority.Invoke(
				this,
				authority => authority.TriggerServerRestart());
#pragma warning restore API1001 // Action returns undeclared success result

		/// <summary>
		/// List <see cref="LogFileResponse"/>s present.
		/// </summary>
		/// <param name="page">The current page.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Listed logs successfully.</response>
		/// <response code="409">An IO error occurred while listing.</response>
		[HttpGet(Routes.Logs)]
		[TgsAuthorize(AdministrationRights.DownloadLogs)]
		[ProducesResponseType(typeof(PaginatedResponse<LogFileResponse>), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 409)]
		public ValueTask<IActionResult> ListLogs([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
			=> Paginated(
				async () =>
				{
					var path = fileLoggingConfiguration.GetFullLogDirectory(ioManager, assemblyInformationProvider, platformIdentifier);
					try
					{
						var files = await ioManager.GetFiles(path, cancellationToken);
						var tasks = files.Select(
							async file => new LogFileResponse
							{
								Name = ioManager.GetFileName(file),
								LastModified = await ioManager
									.GetLastModified(
										ioManager.ConcatPath(path, file),
										cancellationToken),
							})
							.ToList();

						await Task.WhenAll(tasks);

						return new PaginatableResult<LogFileResponse>(
							tasks
								.AsQueryable()
								.Select(x => x.Result)
								.OrderByDescending(x => x.Name));
					}
					catch (IOException ex)
					{
						return new PaginatableResult<LogFileResponse>(
							Conflict(new ErrorMessageResponse(ErrorCode.IOError)
							{
								AdditionalData = ex.ToString(),
							}));
					}
				},
				null,
				page,
				pageSize,
				cancellationToken);

		/// <summary>
		/// Download a <see cref="LogFileResponse"/>.
		/// </summary>
		/// <param name="path">The path to download.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the request.</returns>
		/// <response code="200">Downloaded <see cref="LogFileResponse"/> successfully.</response>
		/// <response code="409">An IO error occurred while downloading.</response>
		[HttpGet(Routes.Logs + "/{*path}")]
		[TgsAuthorize(AdministrationRights.DownloadLogs)]
		[ProducesResponseType(typeof(LogFileResponse), 200)]
		[ProducesResponseType(typeof(ErrorMessageResponse), 409)]
		public ValueTask<IActionResult> GetLog(string path, CancellationToken cancellationToken)
			=> administrationAuthority.Invoke<LogFileResponse, LogFileResponse>(
				this,
				authority => authority.GetLog(path, cancellationToken));
	}
}
