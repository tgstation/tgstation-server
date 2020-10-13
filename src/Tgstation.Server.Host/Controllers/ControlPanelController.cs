using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Net.Mime;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Controller for the web control panel.
	/// </summary>
	[Route(Application.ControlPanelRoute)]
	public class ControlPanelController : Controller
	{
		/// <summary>
		/// The <see cref="IWebHostEnvironment"/> for the <see cref="ControlPanelController"/>.
		/// </summary>
		readonly IWebHostEnvironment hostEnvironment;

		/// <summary>
		/// Initializes a new instance of the <see cref="ControlPanelController"/> <see langword="class"/>.
		/// </summary>
		/// <param name="hostEnvironment">The value of <see cref="hostEnvironment"/>.</param>
		public ControlPanelController(IWebHostEnvironment hostEnvironment)
		{
			this.hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
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
				{
					contentType = "application/octet-stream";
				}

				return File(appRoute, contentType);
			}

			return File("index.html", MediaTypeNames.Text.Html);
		}
	}
}
