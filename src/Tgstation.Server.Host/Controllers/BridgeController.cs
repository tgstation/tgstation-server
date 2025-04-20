using System;
using System.Net;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Serilog.Context;

using Tgstation.Server.Api;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="Controller"/> for receiving DMAPI requests from DreamDaemon.
	/// </summary>
	[Route("/" + RouteExtension)] // obsolete route, but BYOND can't handle a simple fucking 301
	[Route(Routes.ApiRoot + RouteExtension)]
	[ApiExplorerSettings(IgnoreApi = true)]
	public sealed class BridgeController : ApiControllerBase
	{
		/// <summary>
		/// The route to the <see cref="BridgeController"/>.
		/// </summary>
		const string RouteExtension = "Bridge";

		/// <summary>
		/// If the content of bridge requests and responses should be logged.
		/// </summary>
		static bool LogContent => logContentDisableCounter == 0;

		/// <summary>
		/// Counter which, if not zero, indicates content logging should be disabled.
		/// </summary>
		static uint logContentDisableCounter;

		/// <summary>
		/// Static counter for the number of requests processed.
		/// </summary>
		static long requestsProcessed;

		/// <summary>
		/// The <see cref="IBridgeDispatcher"/> for the <see cref="BridgeController"/>.
		/// </summary>
		readonly IBridgeDispatcher bridgeDispatcher;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="BridgeController"/>.
		/// </summary>
		readonly ILogger<BridgeController> logger;

		/// <summary>
		/// Temporarily disable content logging. Must be followed up with a call to <see cref="ReenableContentLogging"/>.
		/// </summary>
		internal static void TemporarilyDisableContentLogging() => Interlocked.Increment(ref logContentDisableCounter);

		/// <summary>
		/// Reenable content logging. Must be preceeded with a call to <see cref="TemporarilyDisableContentLogging"/>.
		/// </summary>
		internal static void ReenableContentLogging() => Interlocked.Decrement(ref logContentDisableCounter);

		/// <summary>
		/// Initializes a new instance of the <see cref="BridgeController"/> class.
		/// </summary>
		/// <param name="bridgeDispatcher">The value of <see cref="bridgeDispatcher"/>.</param>
		/// <param name="applicationLifetime">The <see cref="IHostApplicationLifetime"/> of the server.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public BridgeController(IBridgeDispatcher bridgeDispatcher, IHostApplicationLifetime applicationLifetime, ILogger<BridgeController> logger)
		{
			this.bridgeDispatcher = bridgeDispatcher ?? throw new ArgumentNullException(nameof(bridgeDispatcher));
			ArgumentNullException.ThrowIfNull(applicationLifetime);

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			applicationLifetime.ApplicationStopped.Register(() => requestsProcessed = 0);
		}

		/// <summary>
		/// Processes a bridge request.
		/// </summary>
		/// <param name="data">JSON encoded <see cref="BridgeParameters"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		[HttpGet]
		[AllowAnonymous]
		public async ValueTask<IActionResult> Process([FromQuery] string data, CancellationToken cancellationToken)
		{
			using (LogContext.PushProperty(SerilogContextHelper.BridgeRequestIterationContextProperty, Interlocked.Increment(ref requestsProcessed)))
			{
				// Nothing to see here
				var remoteIP = Request.HttpContext.Connection.RemoteIpAddress;
				if (remoteIP == null || !IPAddress.IsLoopback(remoteIP))
				{
					logger.LogTrace("Rejecting remote bridge request from {remoteIP}", remoteIP);
					return Forbid();
				}

				BridgeParameters? request;
				try
				{
					request = JsonConvert.DeserializeObject<BridgeParameters>(data, DMApiConstants.SerializerSettings);
				}
				catch (Exception ex)
				{
					if (LogContent)
						logger.LogWarning(ex, "Error deserializing bridge request: {badJson}", data);
					else
						logger.LogWarning(ex, "Error deserializing bridge request!");

					return BadRequest();
				}

				if (request == null)
				{
					if (LogContent)
						logger.LogWarning("Error deserializing bridge request: {badJson}", data);
					else
						logger.LogWarning("Error deserializing bridge request!");

					return BadRequest();
				}

				if (LogContent)
					logger.LogTrace("Bridge Request: {json}", data);
				else
					logger.LogTrace("Bridge Request");

				var response = await bridgeDispatcher.ProcessBridgeRequest(request, cancellationToken);
				if (response == null)
					return Forbid();

				var responseJson = JsonConvert.SerializeObject(response, DMApiConstants.SerializerSettings);

				if (LogContent)
					logger.LogTrace("Bridge Response: {json}", responseJson);
				return Content(responseJson, MediaTypeNames.Application.Json);
			}
		}
	}
}
