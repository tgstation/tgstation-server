using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// The root path <see cref="Controller"/>.
	/// </summary>
	[Route("/")]
	[ApiExplorerSettings(IgnoreApi = true)]
	public sealed class RootController : Controller
	{
		/// <summary>
		/// The route to the TGS logo .svg in the <see cref="Microsoft.AspNetCore.Hosting.IWebHostEnvironment.WebRootPath"/> on Windows.
		/// </summary>
		public const string ProjectLogoSvgRouteWindows = "/0176d5d8b7d307f158e0.svg";

		/// <summary>
		/// The route to the TGS logo .svg in the <see cref="Microsoft.AspNetCore.Hosting.IWebHostEnvironment.WebRootPath"/> on Linux.
		/// </summary>
		public const string ProjectLogoSvgRouteLinux = "/b5616c99bf2052a6bbd7.svg";

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="RootController"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="RootController"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="RootController"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// The <see cref="ControlPanelConfiguration"/> for the <see cref="RootController"/>.
		/// </summary>
		readonly ControlPanelConfiguration controlPanelConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="RootController"/> class.
		/// </summary>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="controlPanelConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="controlPanelConfiguration"/>.</param>
		public RootController(
			IAssemblyInformationProvider assemblyInformationProvider,
			IPlatformIdentifier platformIdentifier,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<ControlPanelConfiguration> controlPanelConfigurationOptions)
		{
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			controlPanelConfiguration = controlPanelConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(controlPanelConfigurationOptions));
		}

		/// <summary>
		/// Gets the server's homepage.
		/// </summary>
		/// <returns>The appropriate <see cref="IActionResult"/>.</returns>
		[HttpGet]
		[AllowAnonymous]
		public IActionResult Index()
		{
			const string ApiDocumentationRoute = "/" + SwaggerConfiguration.DocumentationSiteRouteExtension;
			var panelEnabled = controlPanelConfiguration.Enable;
			var apiDocsEnabled = generalConfiguration.HostApiDocumentation;

			if (panelEnabled ^ apiDocsEnabled)
				if (panelEnabled)
					return Redirect(ControlPanelController.ControlPanelRoute);
				else
					return Redirect(ApiDocumentationRoute);

			Dictionary<string, string> links;
			if (panelEnabled)
				links = new Dictionary<string, string>()
				{
					{ "Web Control Panel", ControlPanelController.ControlPanelRoute.TrimStart('/') },
					{ "API Documentation", SwaggerConfiguration.DocumentationSiteRouteExtension },
				};
			else
				links = null;

			var model = new
			{
				Links = links,
				Svg = platformIdentifier.IsWindows // these are different because of motherfucking line endings -_-
					? ProjectLogoSvgRouteWindows
					: ProjectLogoSvgRouteLinux,
				Title = assemblyInformationProvider.VersionString,
			};

			return View(model);
		}
	}
}
