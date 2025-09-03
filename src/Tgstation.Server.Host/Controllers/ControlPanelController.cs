using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mime;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

using Tgstation.Server.Api;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Controller for the web control panel.
	/// </summary>
	[Route(ControlPanelRoute)]
	[ApiExplorerSettings(IgnoreApi = true)]
	public class ControlPanelController : Controller
	{
		/// <summary>
		/// Route to the <see cref="ControlPanelController"/>.
		/// </summary>
		public const string ControlPanelRoute = "/app";

		/// <summary>
		/// The route to the control panel channel .json.
		/// </summary>
		public const string ChannelJsonRoute = "channel.json";

		/// <summary>
		/// Header for forcing channel.json to be fetched.
		/// </summary>
		const string FetchChannelVaryHeader = "X-Webpanel-Fetch-Channel";

		/// <summary>
		/// The <see cref="IWebHostEnvironment"/> for the <see cref="ControlPanelController"/>.
		/// </summary>
		readonly IWebHostEnvironment hostEnvironment;

		/// <summary>
		/// The <see cref="ControlPanelConfiguration"/> for the <see cref="ControlPanelController"/>.
		/// </summary>
		readonly IOptionsSnapshot<ControlPanelConfiguration> controlPanelConfigurationOptions;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ControlPanelController"/>.
		/// </summary>
		readonly ILogger<ControlPanelController> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="ControlPanelController"/> class.
		/// </summary>
		/// <param name="hostEnvironment">The value of <see cref="hostEnvironment"/>.</param>
		/// <param name="controlPanelConfigurationOptions">The value of <see cref="controlPanelConfigurationOptions"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public ControlPanelController(
			IWebHostEnvironment hostEnvironment,
			IOptionsSnapshot<ControlPanelConfiguration> controlPanelConfigurationOptions,
			ILogger<ControlPanelController> logger)
		{
			this.hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
			this.controlPanelConfigurationOptions = controlPanelConfigurationOptions ?? throw new ArgumentNullException(nameof(controlPanelConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Returns the <see cref="ControlPanelConfiguration.Channel"/>.
		/// </summary>
		/// <returns>A <see cref="JsonResult"/> with the <see cref="ControlPanelConfiguration.Channel"/>.</returns>
		[Route(ChannelJsonRoute)]
		[HttpGet]
		public IActionResult GetChannelJson()
		{
			if (!controlPanelConfigurationOptions.Value.Enable)
			{
				logger.LogDebug("Not serving channel.json as control panel is disabled.");
				return NotFound();
			}

			var controlPanelChannel = controlPanelConfigurationOptions.Value.Channel;
			logger.LogTrace("Generating channel.json for channel \"{channel}\"...", controlPanelChannel);

			if (controlPanelChannel == "local")
				controlPanelChannel = ControlPanelRoute;
			else if (String.IsNullOrWhiteSpace(controlPanelChannel))
				controlPanelChannel = null;
			else
				controlPanelChannel = controlPanelChannel
					.Replace("${Major}", ApiHeaders.Version.Major.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
					.Replace("${Minor}", ApiHeaders.Version.Minor.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
					.Replace("${Patch}", ApiHeaders.Version.Build.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

			return Json(new
			{
				FormatVersion = 1,
				Channel = controlPanelChannel,
				controlPanelConfigurationOptions.Value.PublicPath,
			});
		}

		/// <inheritdoc />
		public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			ArgumentNullException.ThrowIfNull(context);

			var newValues = new List<string> { FetchChannelVaryHeader };
			var headers = context.HttpContext.Response.Headers;
			if (headers.TryGetValue(HeaderNames.Vary, out var oldValues))
				headers.Remove(HeaderNames.Vary);

			newValues.AddRange(oldValues);

			headers.Add(HeaderNames.Vary, new StringValues(newValues.ToArray()));

			return base.OnActionExecutionAsync(context, next);
		}

		/// <summary>
		/// Handle a GET request to the control panel route. Route to static files if they exist, otherwise index.html.
		/// </summary>
		/// <param name="appRoute">The value of the route.</param>
		/// <returns>The <see cref="VirtualFileResult"/> to use.</returns>
		[Route("{**appRoute}")]
		[HttpGet]
		public IActionResult Get([FromRoute] string appRoute)
		{
			if (!controlPanelConfigurationOptions.Value.Enable)
			{
				logger.LogDebug("Not serving static files as control panel is disabled.");
				return NotFound();
			}

			if (Request.Headers.ContainsKey(FetchChannelVaryHeader))
				return GetChannelJson();

			var foundFile = this.TryServeFile(hostEnvironment, logger, appRoute);
			if (foundFile != null)
				return foundFile;

			logger.LogTrace("Requested static file \"{filename}\" does not exist! Redirecting to index...", appRoute);

			return File("index.html", MediaTypeNames.Text.Html);
		}
	}
}
