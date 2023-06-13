using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mime;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

using Tgstation.Server.Api;
using Tgstation.Server.Host.Configuration;

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
		readonly ControlPanelConfiguration controlPanelConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="ControlPanelController"/> class.
		/// </summary>
		/// <param name="hostEnvironment">The value of <see cref="hostEnvironment"/>.</param>
		/// <param name="controlPanelConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="controlPanelConfiguration"/>.</param>
		public ControlPanelController(IWebHostEnvironment hostEnvironment, IOptions<ControlPanelConfiguration> controlPanelConfigurationOptions)
		{
			this.hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
			controlPanelConfiguration = controlPanelConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(controlPanelConfigurationOptions));
		}

		/// <summary>
		/// Returns the <see cref="ControlPanelConfiguration.Channel"/>.
		/// </summary>
		/// <returns>A <see cref="JsonResult"/> with the <see cref="ControlPanelConfiguration.Channel"/>.</returns>
		[Route("channel.json")]
		[HttpGet]
		public IActionResult GetChannelJson()
		{
			if (!controlPanelConfiguration.Enable)
				return NotFound();

			var controlPanelChannel = controlPanelConfiguration.Channel;
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
				controlPanelConfiguration.PublicPath,
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
			if (!controlPanelConfiguration.Enable)
				return NotFound();

			if (Request.Headers.ContainsKey(FetchChannelVaryHeader))
				return GetChannelJson();

			var fileInfo = hostEnvironment.WebRootFileProvider.GetFileInfo(appRoute);
			if (fileInfo.Exists)
			{
				var contentTypeProvider = new FileExtensionContentTypeProvider();
				if (!contentTypeProvider.TryGetContentType(fileInfo.Name, out var contentType))
					contentType = MediaTypeNames.Application.Octet;
				else if (contentType == MediaTypeNames.Application.Json)
					Response.Headers.Add(
						HeaderNames.CacheControl,
						new StringValues(new[] { "public", "max-age=31536000", "immutable" }));

				return File(appRoute, contentType);
			}

			return File("index.html", MediaTypeNames.Text.Html);
		}
	}
}
