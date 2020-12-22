using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;
using System;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Swarm;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// For swarm server communication.
	/// </summary>
	[Route(SwarmConstants.ControllerRoute)]
	[Produces(MediaTypeNames.Application.Json)]
	[ApiController]
	public sealed class SwarmController : Controller
	{
		/// <summary>
		/// Get the current registration <see cref="Guid"/> from the <see cref="ControllerBase.Request"/>.
		/// </summary>
		Guid RequestRegistrationId => Guid.Parse(Request.Headers[SwarmConstants.RegistrationIdHeader].First());

		/// <summary>
		/// The <see cref="ISwarmOperations"/> for the <see cref="SwarmController"/>.
		/// </summary>
		readonly ISwarmOperations swarmOperations;

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
		/// Initializes a new instance of the <see cref="SwarmController"/> <see langword="class"/>.
		/// </summary>
		/// <param name="swarmOperations">The value of <see cref="swarmOperations"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="swarmConfiguration"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public SwarmController(
			ISwarmOperations swarmOperations,
			IAssemblyInformationProvider assemblyInformationProvider,
			IOptions<SwarmConfiguration> swarmConfigurationOptions,
			ILogger<SwarmController> logger)
		{
			this.swarmOperations = swarmOperations ?? throw new ArgumentNullException(nameof(swarmOperations));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			swarmConfiguration = swarmConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions));
			this.logger = logger;
		}

		/// <summary>
		/// Check that the <see cref="RequestRegistrationId"/> is valid.
		/// </summary>
		/// <returns><see langword="true"/> if the <see cref="RequestRegistrationId"/> is valid, <see langword="false"/> otherwise.</returns>
		bool ValidateRegistration() => swarmOperations.ValidateRegistration(RequestRegistrationId);

		/// <summary>
		/// Registration endpoint.
		/// </summary>
		/// <param name="registrationRequest">The <see cref="SwarmRegistrationRequest"/>.</param>
		/// <returns>The <see cref="IActionResult"/> of the operation.</returns>
		[HttpPost(SwarmConstants.RegisterRoute)]
		public IActionResult Register([FromBody]SwarmRegistrationRequest registrationRequest)
		{
			if (registrationRequest == null)
				throw new ArgumentNullException(nameof(registrationRequest));

			if (registrationRequest.ServerVersion != assemblyInformationProvider.Version)
				return StatusCode((int)HttpStatusCode.UpgradeRequired);

			var registrationResult = swarmOperations.RegisterNode(registrationRequest, RequestRegistrationId);
			if (!registrationResult)
				return Conflict();
			return NoContent();
		}

		/// <summary>
		/// Deregistration endpoint.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		[HttpDelete(SwarmConstants.RegisterRoute)]
		public async Task<IActionResult> UnregisterNode(CancellationToken cancellationToken)
		{
			if (!ValidateRegistration())
				return Forbid();

			await swarmOperations.UnregisterNode(RequestRegistrationId, cancellationToken).ConfigureAwait(false);
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
		/// Node list update endpoint.
		/// </summary>
		/// <param name="serversUpdateRequest">The <see cref="SwarmServersUpdateRequest"/>.</param>
		/// <returns>The <see cref="IActionResult"/> of the operation.</returns>
		[HttpPost]
		public IActionResult UpdateNodeList([FromBody]SwarmServersUpdateRequest serversUpdateRequest)
		{
			if (serversUpdateRequest == null)
				throw new ArgumentNullException(nameof(serversUpdateRequest));

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
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		[HttpPut(SwarmConstants.UpdateRoute)]
		public async Task<IActionResult> PrepareUpdate([FromBody]SwarmUpdateRequest updateRequest, CancellationToken cancellationToken)
		{
			if (updateRequest == null)
				throw new ArgumentNullException(nameof(updateRequest));

			if (!ValidateRegistration())
				return Forbid();

			var prepareResult = await swarmOperations.PrepareUpdateFromController(updateRequest.UpdateVersion, cancellationToken).ConfigureAwait(false);
			if (!prepareResult)
				return Conflict();

			return NoContent();
		}

		/// <summary>
		/// Update commit endpoint.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		[HttpPost(SwarmConstants.UpdateRoute)]
		public async Task<IActionResult> CommitUpdate(CancellationToken cancellationToken)
		{
			if (!ValidateRegistration())
				return Forbid();

			var result = await swarmOperations.RemoteCommitRecieved(RequestRegistrationId, cancellationToken).ConfigureAwait(false);
			if (!result)
				return Conflict();
			return NoContent();
		}

		/// <summary>
		/// Update abort endpoint.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation.</returns>
		[HttpDelete(SwarmConstants.UpdateRoute)]
		public async Task<IActionResult> AbortUpdate(CancellationToken cancellationToken)
		{
			if (!ValidateRegistration())
				return Forbid();

			await swarmOperations.AbortUpdate(cancellationToken).ConfigureAwait(false);
			return NoContent();
		}

		/// <inheritdoc />
		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			if (swarmConfiguration.PrivateKey == null)
			{
				logger.LogDebug("Attempted swarm request without private key!");
				await Forbid().ExecuteResultAsync(context).ConfigureAwait(false);
				return;
			}

			if (!(Request.Headers.TryGetValue(SwarmConstants.ApiKeyHeader, out var apiKeyHeaderValues)
				&& apiKeyHeaderValues.Count == 1
				&& apiKeyHeaderValues.First() == swarmConfiguration.PrivateKey))
			{
				logger.LogDebug("Unauthorized swarm request!");
				await Unauthorized().ExecuteResultAsync(context).ConfigureAwait(false);
				return;
			}

			if (!(Request.Headers.TryGetValue(SwarmConstants.RegistrationIdHeader, out var registrationHeaderValues)
				&& registrationHeaderValues.Count == 1
				&& Guid.TryParse(registrationHeaderValues.First(), out var registrationId)))
			{
				logger.LogDebug("Swarm request without registration ID!");
				await BadRequest().ExecuteResultAsync(context).ConfigureAwait(false);
				return;
			}

			// we validate the registration itself on a case-by-case basis
			if (ModelState?.IsValid == false)
			{
				var errors = ModelState
					.SelectMany(x => x.Value.Errors)
					.Select(x => x.Exception);

				logger.LogDebug(new AggregateException(errors), "Swarm request model validation failed!");
				await BadRequest().ExecuteResultAsync(context).ConfigureAwait(false);
				return;
			}

			using (LogContext.PushProperty("Request", $"{Request.Method} {Request.Path}"))
			{
				logger.LogDebug("Starting swarm request...");
				await base.OnActionExecutionAsync(context, next).ConfigureAwait(false);
			}
		}
	}
}
