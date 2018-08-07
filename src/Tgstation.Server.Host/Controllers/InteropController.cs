using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Handles requests from DreamDaemon
	/// </summary>
	[Route("/Interop")]
	public sealed class InteropController : Controller
	{
		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="InteropController"/>
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="InteropController"/>
		/// </summary>
		readonly ILogger<InteropController> logger;

		/// <summary>
		/// Construct an <see cref="InteropController"/>
		/// </summary>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public InteropController(IInstanceManager instanceManager, ILogger<InteropController> logger)
		{
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Handle a GET to the <see cref="InteropController"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the operation</returns>
		[HttpGet]
		public async Task<IActionResult> HandleInterop(CancellationToken cancellationToken)
		{
			//since this is the only identifying factor of a TGS server we want to pretend we don't exist unless it at least has the correct BYOND headers
			if (!Request.Headers.TryGetValue(HeaderNames.UserAgent, out var userAgentValues) 
				|| !ProductInfoHeaderValue.TryParse(userAgentValues.FirstOrDefault(), out var clientUserAgent)
				|| clientUserAgent.Product.Name != "libbyond")
				return Unauthorized();

			logger.LogDebug("Request from BYOND: {0}", Request.QueryString);

			var result = await instanceManager.HandleWorldExport(Request.Query, cancellationToken).ConfigureAwait(false);
			//explain things in very simple terms dream daemon can understand	
			//EXCEPT DREAMDAENEN BUGS
			//THAT"S RIGHT !BUGS!
			//ON ANYTHING THAT ISNT A 200 RESPOSNCE REEEE
			if (result == null)
				result = new { STATUS = 404 };
			return Json(result);
		}
	}
}
