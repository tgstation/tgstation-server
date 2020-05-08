using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="Controller"/> for recieving DMAPI requests from DreamDaemon.
	/// </summary>
	[Route("Bridge")]
	[Produces(ApiHeaders.ApplicationJson)]
	public class BridgeController : Controller
	{
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
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public BridgeController(IBridgeDispatcher bridgeDispatcher, ILogger<BridgeController> logger)
		{
			this.bridgeDispatcher = bridgeDispatcher ?? throw new ArgumentNullException(nameof(bridgeDispatcher));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
			if (!IPAddress.IsLoopback(Request.HttpContext.Connection.RemoteIpAddress))
				return NotFound();

			BridgeParameters request;
			try
			{
				request = JsonConvert.DeserializeObject<BridgeParameters>(data, DMApiConstants.SerializerSettings);
			}
			catch
			{
				logger.LogWarning("Error deserializing bridge request: {0}", data);
				return BadRequest();
			}

			logger.LogTrace("Bridge Request: {0}", data);

			var response = await bridgeDispatcher.ProcessBridgeRequest(request, cancellationToken).ConfigureAwait(false);
			if (response == null)
				Forbid();

			var responseJson = JsonConvert.SerializeObject(response, DMApiConstants.SerializerSettings);
			logger.LogTrace("Bridge Response: {0}", responseJson);
			return Content(responseJson, ApiHeaders.ApplicationJson);
		}
	}
}
