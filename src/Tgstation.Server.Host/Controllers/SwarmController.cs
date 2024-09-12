using System;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Serilog.Context;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Swarm;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Transfer;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// For swarm server communication.
	/// </summary>
	[Route(SwarmConstants.ControllerRoute)]
	[ApiExplorerSettings(IgnoreApi = true)]
	[AllowAnonymous] // We have custom private key auth
	public sealed class SwarmController : ApiControllerBase
	{
		/// <summary>
		/// Get the current registration <see cref="Guid"/> from the <see cref="ControllerBase.Request"/>.
		/// </summary>
		internal Guid RequestRegistrationId => Guid.Parse(Request.Headers[SwarmConstants.RegistrationIdHeader].First()!);

		/// <summary>
		/// The <see cref="ISwarmOperations"/> for the <see cref="SwarmController"/>.
		/// </summary>
		readonly ISwarmOperations swarmOperations;

		/// <summary>
		/// The <see cref="IFileTransferStreamHandler"/> for the <see cref="SwarmController"/>.
		/// </summary>
		readonly IFileTransferStreamHandler transferService;

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="SwarmController"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="SwarmController"/>.
		/// </summary>
		readonly ILogger<SwarmController> logger;

		/// <summary>
		/// The <see cref="SwarmConfiguration"/> for the <see cref="SwarmController"/>.
		/// </summary>
		readonly SwarmConfiguration swarmConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmController"/> class.
		/// </summary>
		/// <param name="swarmOperations">The value of <see cref="swarmOperations"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="transferService">The value of <see cref="transferService"/>.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="swarmConfiguration"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public SwarmController(
			ISwarmOperations swarmOperations,
			IAssemblyInformationProvider assemblyInformationProvider,
			IFileTransferStreamHandler transferService,
			IOptions<SwarmConfiguration> swarmConfigurationOptions,
			ILogger<SwarmController> logger)
		{
			this.swarmOperations = swarmOperations ?? throw new ArgumentNullException(nameof(swarmOperations));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.transferService = transferService ?? throw new ArgumentNullException(nameof(transferService));
			swarmConfiguration = swarmConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions));
			this.logger = logger;
		}

		/// <summary>
		/// Registration endpoint.
		/// </summary>
		/// <param name="registrationRequest">The <see cref="SwarmRegistrationRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The <see cref="IActionResult"/> of the operation.</returns>
		[HttpPost(SwarmConstants.RegisterRoute)]
		public async ValueTask<IActionResult> Register([FromBody] SwarmRegistrationRequest registrationRequest, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(registrationRequest);

			if (registrationRequest.ServerVersion != assemblyInformationProvider.Version)
				return StatusCode((int)HttpStatusCode.UpgradeRequired);

			var registrationResult = await swarmOperations.RegisterNode(registrationRequest, RequestRegistrationId, cancellationToken);
			if (registrationResult == null)
				return Conflict();
			return Json(registrationResult);
		}

		/// <summary>
		/// Deregistration endpoint.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		[HttpDelete(SwarmConstants.RegisterRoute)]
		public async ValueTask<IActionResult> UnregisterNode(CancellationToken cancellationToken)
		{
			if (!ValidateRegistration())
				return Forbid();

			await swarmOperations.UnregisterNode(RequestRegistrationId, cancellationToken);
			return NoContent();
		}

		/// <summary>
		/// Health check endpoint.
		/// </summary>
		/// <returns>The <see cref="IActionResult"/> of the operation.</returns>
		[HttpGet]
		public IActionResult HealthCheck()
		{
			if (!ValidateRegistration())
				return Forbid();

			return NoContent();
		}

		/// <summary>
		/// Endpoint to retrieve server update packages.
		/// </summary>
		/// <param name="ticket">The <see cref="Api.Models.Response.FileTicketResponse.FileTicket"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		[HttpGet(SwarmConstants.UpdateRoute)]
		[Produces(MediaTypeNames.Application.Octet)]
		public ValueTask<IActionResult> GetUpdatePackage([FromQuery] string ticket, CancellationToken cancellationToken)
			=> transferService.GenerateDownloadResponse(this, ticket, cancellationToken);

		/// <summary>
		/// Node list update endpoint.
		/// </summary>
		/// <param name="serversUpdateRequest">The <see cref="SwarmServersUpdateRequest"/>.</param>
		/// <returns>The <see cref="IActionResult"/> of the operation.</returns>
		[HttpPost]
		public IActionResult UpdateNodeList([FromBody] SwarmServersUpdateRequest serversUpdateRequest)
		{
			ArgumentNullException.ThrowIfNull(serversUpdateRequest);

			if (serversUpdateRequest.SwarmServers == null)
				return BadRequest();

			if (!ValidateRegistration())
				return Forbid();

			swarmOperations.UpdateSwarmServersList(serversUpdateRequest.SwarmServers);
			return NoContent();
		}

		/// <summary>
		/// Update initiation endpoint.
		/// </summary>
		/// <param name="updateRequest">The <see cref="SwarmUpdateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		[HttpPut(SwarmConstants.UpdateRoute)]
		public async ValueTask<IActionResult> PrepareUpdate([FromBody] SwarmUpdateRequest updateRequest, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(updateRequest);

			if (!ValidateRegistration())
				return Forbid();

			var prepareResult = await swarmOperations.PrepareUpdateFromController(updateRequest, cancellationToken);
			if (!prepareResult)
				return Conflict();

			return NoContent();
		}

		/// <summary>
		/// Update commit endpoint.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		[HttpPost(SwarmConstants.UpdateRoute)]
		public async ValueTask<IActionResult> CommitUpdate(CancellationToken cancellationToken)
		{
			if (!ValidateRegistration())
				return Forbid();

			var result = await swarmOperations.RemoteCommitReceived(RequestRegistrationId, cancellationToken);
			if (!result)
				return Conflict();
			return NoContent();
		}

		/// <summary>
		/// Update abort endpoint.
		/// </summary>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		[HttpDelete(SwarmConstants.UpdateRoute)]
		public async ValueTask<IActionResult> AbortUpdate()
		{
			if (!ValidateRegistration())
				return Forbid();

			await swarmOperations.AbortUpdate();
			return NoContent();
		}

		/// <inheritdoc />
		protected override async ValueTask<IActionResult?> HookExecuteAction(Func<Task> executeAction, CancellationToken cancellationToken)
		{
			using (LogContext.PushProperty(SerilogContextHelper.RequestPathContextProperty, $"{Request.Method} {Request.Path}"))
			{
				logger.LogTrace("Swarm request from {remoteIP}...", Request.HttpContext.Connection.RemoteIpAddress);
				if (swarmConfiguration.PrivateKey == null)
				{
					logger.LogDebug("Attempted swarm request without private key configured!");
					return Forbid();
				}

				if (!(Request.Headers.TryGetValue(SwarmConstants.ApiKeyHeader, out var apiKeyHeaderValues)
					&& apiKeyHeaderValues.Count == 1
					&& apiKeyHeaderValues.First() == swarmConfiguration.PrivateKey))
				{
					logger.LogDebug("Unauthorized swarm request!");
					return Unauthorized();
				}

				if (!(Request.Headers.TryGetValue(SwarmConstants.RegistrationIdHeader, out var registrationHeaderValues)
					&& registrationHeaderValues.Count == 1
					&& Guid.TryParse(registrationHeaderValues.First(), out var registrationId)))
				{
					logger.LogDebug("Swarm request without registration ID!");
					return BadRequest();
				}

				// we validate the registration itself on a case-by-case basis
				if (ModelState?.IsValid == false)
				{
					var errorMessages = ModelState
						.SelectMany(x => x.Value!.Errors)
						.Select(x => x.ErrorMessage);

					logger.LogDebug(
						"Swarm request model validation failed!{newLine}{messages}",
						Environment.NewLine,
						String.Join(Environment.NewLine, errorMessages));
					return BadRequest();
				}

				logger.LogTrace("Starting swarm request processing...");
				await executeAction();
				return null;
			}
		}

		/// <summary>
		/// Check that the <see cref="RequestRegistrationId"/> is valid.
		/// </summary>
		/// <returns><see langword="true"/> if the <see cref="RequestRegistrationId"/> is valid, <see langword="false"/> otherwise.</returns>
		bool ValidateRegistration() => swarmOperations.ValidateRegistration(RequestRegistrationId);
	}
}
