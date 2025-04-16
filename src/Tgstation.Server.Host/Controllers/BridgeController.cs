using System;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Serilog.Context;

using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// <see cref="Controller"/> for receiving DMAPI requests from game servers.
	/// </summary>
	public sealed class BridgeController
	{
		/// <summary>
		/// The route to the <see cref="BridgeController"/>.
		/// </summary>
		public const string RouteExtension = "Bridge";

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
		public BridgeController(
			IBridgeDispatcher bridgeDispatcher,
			IHostApplicationLifetime applicationLifetime,
			ILogger<BridgeController> logger)
		{
			this.bridgeDispatcher = bridgeDispatcher ?? throw new ArgumentNullException(nameof(bridgeDispatcher));
			ArgumentNullException.ThrowIfNull(applicationLifetime);

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			applicationLifetime.ApplicationStopped.Register(() => requestsProcessed = 0);
		}

		/// <summary>
		/// Processes a bridge request.
		/// </summary>
		/// <param name="context">The request's <see cref="HttpContext"/>.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		public async Task Process(HttpContext context)
		{
			ArgumentNullException.ThrowIfNull(context);

			string? data;
			if (context.Request.Query.TryGetValue(nameof(data), out var values))
			{
				if (values.Count > 1)
				{
					logger.LogWarning("Bridge request returned {0} data query parameters!", values.Count);
					await TypedResults.BadRequest().ExecuteAsync(context);
					return;
				}

				data = values.FirstOrDefault();
			}
			else
				data = null;

			var result = await ProcessImpl(data, context.RequestAborted);

			await result.ExecuteAsync(context);
		}

		/// <summary>
		/// Processes a bridge request.
		/// </summary>
		/// <param name="data">JSON encoded <see cref="BridgeParameters"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		private async ValueTask<IResult> ProcessImpl(string? data, CancellationToken cancellationToken)
		{
			using (LogContext.PushProperty(SerilogContextHelper.BridgeRequestIterationContextProperty, Interlocked.Increment(ref requestsProcessed)))
			{
				// Nothing to see here
				if (String.IsNullOrEmpty(data))
				{
					logger.LogWarning("Bridge request performed without data!");
					return TypedResults.BadRequest();
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

					return TypedResults.BadRequest();
				}

				if (request == null)
				{
					if (LogContent)
						logger.LogWarning("Error deserializing bridge request: {badJson}", data);
					else
						logger.LogWarning("Error deserializing bridge request!");

					return TypedResults.BadRequest();
				}

				if (LogContent)
					logger.LogTrace("Bridge Request: {json}", data);
				else
					logger.LogTrace("Bridge Request");

				var response = await bridgeDispatcher.ProcessBridgeRequest(request, cancellationToken);
				if (response == null)
					TypedResults.Forbid();

				var responseJson = JsonConvert.SerializeObject(response, DMApiConstants.SerializerSettings);

				if (LogContent)
					logger.LogTrace("Bridge Response: {json}", responseJson);
				return TypedResults.Content(responseJson, MediaTypeNames.Application.Json);
			}
		}
	}
}
