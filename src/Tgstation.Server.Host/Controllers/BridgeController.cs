using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Host.Components;
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
		/// The <see cref="IInstanceManager"/> for the <see cref="BridgeController"/>
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="BridgeController"/>
		/// </summary>
		readonly ILogger<BridgeController> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="BridgeController"/> <see langword="class"/>.
		/// </summary>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public BridgeController(IInstanceManager instanceManager, ILogger<BridgeController> logger)
		{
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
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
			if (String.IsNullOrWhiteSpace(data))
				return BadRequest();

			BridgeParameters request;
			try
			{
				request = JsonConvert.DeserializeObject<BridgeParameters>(data, DMApiConstants.SerializerSettings);
			}
			catch
			{
				logger.LogDebug("Error deserializing bridge request: {0}", data);
				return BadRequest();
			}

			logger.LogTrace("Bridge Request: {0}", data);

			var response = await instanceManager.ProcessBridgeRequest(request, cancellationToken).ConfigureAwait(false);
			if (response == null)
				Forbid();

			var responseJson = JsonConvert.SerializeObject(response, DMApiConstants.SerializerSettings);
			return Content(responseJson, ApiHeaders.ApplicationJson);
		}
	}
}
