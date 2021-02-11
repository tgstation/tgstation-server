using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Globalization;
using System.Net.Mime;
using Tgstation.Server.Api;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Controller for the web control panel.
	/// </summary>
	[Route(Application.ControlPanelRoute)]
	[ApiExplorerSettings(IgnoreApi = true)]
	public class ControlPanelController : Controller
	{
		/// <summary>
		/// The <see cref="IWebHostEnvironment"/> for the <see cref="ControlPanelController"/>.
		/// </summary>
		readonly IWebHostEnvironment hostEnvironment;

		/// <summary>
		/// The <see cref="ControlPanelConfiguration"/> for the <see cref="ControlPanelController"/>.
		/// </summary>
		readonly ControlPanelConfiguration controlPanelConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="ControlPanelController"/> <see langword="class"/>.
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
		public JsonResult GetChannelJson()
		{
			var controlPanelChannel = controlPanelConfiguration.Channel;
			if (controlPanelChannel == "local")
				controlPanelChannel = Application.ControlPanelRoute;

			controlPanelChannel = controlPanelChannel
				.Replace("${Major}", ApiHeaders.Version.Major.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
				.Replace("${Minor}", ApiHeaders.Version.Minor.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
				.Replace("${Patch}", ApiHeaders.Version.Build.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

			return Json(new
			{
				FormatVersion = 1,
				Channel = controlPanelChannel,
			});
		}

		/// <summary>
		/// Handle a GET request to the control panel route. Route to static files if they exist, otherwise index.html.
		/// </summary>
		/// <param name="appRoute">The value of the route.</param>
		/// <returns>The <see cref="VirtualFileResult"/> to use.</returns>
		[Route("{**appRoute}")]
		[HttpGet]
		public VirtualFileResult Get([FromRoute] string appRoute)
		{
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
