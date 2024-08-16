using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;
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
		/// The name of the TGS logo .svg in the <see cref="IWebHostEnvironment.WebRootPath"/> on Windows.
		/// </summary>
		const string LogoSvgWindowsName = "0176d5d8b7d307f158e0";

		/// <summary>
		/// The name of  the TGS logo .svg in the <see cref="IWebHostEnvironment.WebRootPath"/> on Linux.
		/// </summary>
		const string LogoSvgLinuxName = "b5616c99bf2052a6bbd7";

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="RootController"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="RootController"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// THe <see cref="IWebHostEnvironment"/> for the <see cref="RootController"/>.
		/// </summary>
		readonly IWebHostEnvironment hostEnvironment;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="RootController"/>.
		/// </summary>
		readonly ILogger<RootController> logger;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="RootController"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// The <see cref="ControlPanelConfiguration"/> for the <see cref="RootController"/>.
		/// </summary>
		readonly ControlPanelConfiguration controlPanelConfiguration;

		/// <summary>
		/// Gets a <see cref="Tuple{T1, T2}"/> giving the <see cref="ControlPanelController"/> and action names for a given <paramref name="actionExpression"/>.
		/// </summary>
		/// <param name="actionExpression">An expression invoking the action on <see cref="ControlPanelController"/>.</param>
		/// <returns>A <see cref="Tuple{T1, T2}"/> containing the controller and action names.</returns>
		static Tuple<string, string?> GetControlPanelActionLink(Expression<Func<ControlPanelController, IActionResult>> actionExpression)
		{
			var memberSelectorExpression = (MethodCallExpression)actionExpression.Body;
			var method = memberSelectorExpression.Method;

			var controllerName = typeof(ControlPanelController).Name;

			const string ControllerSuffix = nameof(Controller);
			if (controllerName.EndsWith(ControllerSuffix, StringComparison.Ordinal))
				controllerName = controllerName.Substring(0, controllerName.Length - ControllerSuffix.Length);

			var actionName = method.Name;

			return Tuple.Create<string, string?>(controllerName, actionName);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RootController"/> class.
		/// </summary>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="hostEnvironment">The value of <see cref="hostEnvironment"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="controlPanelConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="controlPanelConfiguration"/>.</param>
		public RootController(
			IAssemblyInformationProvider assemblyInformationProvider,
			IPlatformIdentifier platformIdentifier,
			IWebHostEnvironment hostEnvironment,
			ILogger<RootController> logger,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<ControlPanelConfiguration> controlPanelConfigurationOptions)
		{
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
			var panelEnabled = controlPanelConfiguration.Enable;
			var apiDocsEnabled = generalConfiguration.HostApiDocumentation;

			var controlPanelRoute = $"{ControlPanelController.ControlPanelRoute.TrimStart('/')}/";
			if (panelEnabled ^ apiDocsEnabled)
				if (panelEnabled)
					return Redirect(controlPanelRoute);
				else
					return Redirect(SwaggerConfiguration.DocumentationSiteRouteExtension);

			Dictionary<string, string>? links;
			if (panelEnabled)
				links = new Dictionary<string, string>()
				{
					{ "Web Control Panel", controlPanelRoute },
					{ "API Documentation", SwaggerConfiguration.DocumentationSiteRouteExtension },
				};
			else
				links = null;

			var model = new
			{
				Links = links,
				Title = assemblyInformationProvider.VersionString,
			};

			return View(model);
		}

		/// <summary>
		/// Retrieve the logo .svg for the webpanel.
		/// </summary>
		/// <returns>The appropriate <see cref="IActionResult"/>.</returns>
		[HttpGet("logo.svg")]
		public IActionResult GetLogo()
		{
			// these are different because of motherfucking line endings -_-
			if (platformIdentifier.IsWindows)
			{
				VirtualFileResult? result = this.TryServeFile(hostEnvironment, logger, $"{LogoSvgWindowsName}.svg");
				if (result != null)
					return result;

				// BUT THE UPDATE PACKAGES ARE BUILT ON LINUX RAAAAAGH
			}

			return (IActionResult?)this.TryServeFile(hostEnvironment, logger, $"{LogoSvgLinuxName}.svg") ?? NotFound();
		}

		/// <summary>
		/// Workaround for the webpanel always expecting a trailing slash.
		/// </summary>
		/// <returns>A <see cref="RedirectResult"/> to the <see cref="ControlPanelController.GetChannelJson"/>.</returns>
		[HttpGet(ControlPanelController.ChannelJsonRoute)]
		public RedirectToActionResult WebpanelChannelRedirect()
		{
			var actionTuple = GetControlPanelActionLink(controller => controller.GetChannelJson());

			return RedirectToActionPermanent(
				actionTuple.Item2,
				actionTuple.Item1);
		}
	}
}
