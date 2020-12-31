using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Net;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="Controller"/> for recieving DMAPI requests from DreamDaemon.
	/// </summary>
	[Route("/Bridge")]
	[Produces(MediaTypeNames.Application.Json)]
	[ApiController]
	public class BridgeController : Controller
	{
		/// <summary>
		/// Static counter for the number of requests processed.
		/// </summary>
		static long requestsProcessed;

		/// <summary>
		/// The <see cref="IBridgeDispatcher"/> for the <see cref="BridgeController"/>
		/// </summary>
		readonly IBridgeDispatcher bridgeDispatcher;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="BridgeController"/>
		/// </summary>
		readonly ILogger<BridgeController> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="BridgeController"/> <see langword="class"/>.
		/// </summary>
		/// <param name="bridgeDispatcher">The value of <see cref="bridgeDispatcher"/>.</param>
		/// <param name="applicationLifetime">The <see cref="IHostApplicationLifetime"/> of the server.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public BridgeController(IBridgeDispatcher bridgeDispatcher, IHostApplicationLifetime applicationLifetime, ILogger<BridgeController> logger)
		{
			this.bridgeDispatcher = bridgeDispatcher ?? throw new ArgumentNullException(nameof(bridgeDispatcher));
			if (applicationLifetime == null)
				throw new ArgumentNullException(nameof(applicationLifetime));

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			applicationLifetime.ApplicationStopped.Register(() => requestsProcessed = 0);
		}

		/// <summary>
		/// Processes a bridge request.
		/// </summary>
		/// <param name="data">JSON encoded <see cref="BridgeParameters"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		[HttpGet]
		public async Task<IActionResult> Process([FromQuery]string data, CancellationToken cancellationToken)
		{
			// Nothing to see here
			var remoteIP = Request.HttpContext.Connection.RemoteIpAddress;
			if (!IPAddress.IsLoopback(remoteIP))
			{
				logger.LogTrace("Rejecting remote bridge request from {0}", remoteIP);
				return Forbid();
			}

			using (LogContext.PushProperty("Bridge", Interlocked.Increment(ref requestsProcessed)))
			{
				BridgeParameters request;
				try
				{
					request = JsonConvert.DeserializeObject<BridgeParameters>(data, DMApiConstants.SerializerSettings);
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Error deserializing bridge request: {0}", data);
					return BadRequest();
				}

				logger.LogTrace("Bridge Request: {0}", data);

				var response = await bridgeDispatcher.ProcessBridgeRequest(request, cancellationToken).ConfigureAwait(false);
				if (response == null)
					Forbid();

				var responseJson = JsonConvert.SerializeObject(response, DMApiConstants.SerializerSettings);
				logger.LogTrace("Bridge Response: {0}", responseJson);
				return Content(responseJson, MediaTypeNames.Application.Json);
			}
		}
	}
}
