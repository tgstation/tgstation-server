using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Setup;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host
{
	/// <summary>
	/// Implementation of <see cref="IServerFactory"/>.
	/// </summary>
	sealed class ServerFactory : IServerFactory
	{
		/// <summary>
		/// Name of the appsettings file.
		/// </summary>
		public const string AppSettings = "appsettings";

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="ServerFactory"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <inheritdoc />
		public IIOManager IOManager { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerFactory"/> class.
		/// </summary>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="ioManager">The value of <see cref="IOManager"/>.</param>
		internal ServerFactory(IAssemblyInformationProvider assemblyInformationProvider, IIOManager ioManager)
		{
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			IOManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
		}

		/// <inheritdoc />
		// TODO: Decomplexify
#pragma warning disable CA1506
		public async ValueTask<IServer?> CreateServer(string[] args, string? updatePath, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(args);

			// need to shove this arg in to disable config reloading unless a user specifically overrides it
			if (!args.Any(arg => arg.Contains("hostBuilder:reloadConfigOnChange", StringComparison.OrdinalIgnoreCase))
				&& String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("hostBuilder__reloadConfigOnChange")))
			{
				var oldArgs = args;
				args = new string[oldArgs.Length + 1];
				Array.Copy(oldArgs, args, oldArgs.Length);
				args[oldArgs.Length] = "--hostBuilder:reloadConfigOnChange=false";
			}

			const string AppSettingsRelocationKey = $"--{AppSettings}-base-path=";

			var appsettingsRelativeBasePathArgument = args.FirstOrDefault(arg => arg.StartsWith(AppSettingsRelocationKey, StringComparison.Ordinal));
			string basePath;
			if (appsettingsRelativeBasePathArgument != null)
				basePath = IOManager.ResolvePath(appsettingsRelativeBasePathArgument[AppSettingsRelocationKey.Length..]);
			else
				basePath = IOManager.ResolvePath();

			// this is a massive bloody hack but I don't know a better way to do it
			// It's needed for the setup wizard
			Environment.SetEnvironmentVariable($"{InternalConfiguration.Section}__{nameof(InternalConfiguration.AppSettingsBasePath)}", basePath);

			IHostBuilder CreateDefaultBuilder() => Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
				.ConfigureAppConfiguration((context, builder) =>
				{
					builder.SetBasePath(basePath);

					builder.AddYamlFile($"{AppSettings}.yml", optional: true, reloadOnChange: false)
						.AddYamlFile($"{AppSettings}.{context.HostingEnvironment.EnvironmentName}.yml", optional: true, reloadOnChange: false);

					// reorganize the builder so our yaml configs don't override the env/cmdline configs
					// values obtained via debugger
					var environmentJsonConfig = builder.Sources[2];
					var envConfig = builder.Sources[3];

					// CURSED
					// https://github.com/dotnet/runtime/blob/30dc7e7aedb7aab085c7d9702afeae5bc5a43133/src/libraries/Microsoft.Extensions.Hosting/src/HostingHostBuilderExtensions.cs#L246-L249
#if !NET10_0
#error Validate this monstrosity works on current .NET
#endif
					IConfigurationSource? cmdLineConfig;
					IConfigurationSource baseYmlConfig, environmentYmlConfig;
					if (args.Length == 0)
					{
						cmdLineConfig = null;
						baseYmlConfig = builder.Sources[4];
						environmentYmlConfig = builder.Sources[5];
					}
					else
					{
						cmdLineConfig = builder.Sources[4];
						baseYmlConfig = builder.Sources[5];
						environmentYmlConfig = builder.Sources[6];
					}

					builder.Sources[2] = baseYmlConfig;
					builder.Sources[3] = environmentJsonConfig;
					builder.Sources[4] = environmentYmlConfig;
					builder.Sources[5] = envConfig;

					if (cmdLineConfig != null)
					{
						builder.Sources[6] = cmdLineConfig;
					}
				});

			var setupWizardHostBuilder = CreateDefaultBuilder()
				.UseSetupApplication(assemblyInformationProvider, IOManager);

			IPostSetupServices postSetupServices;
			using (var setupHost = setupWizardHostBuilder.Build())
			{
				ILogger<ServerFactory> logger = setupHost.Services.GetRequiredService<ILogger<ServerFactory>>();
				postSetupServices = setupHost.Services.GetRequiredService<IPostSetupServices>();
				await setupHost.RunAsync(cancellationToken);

				if (postSetupServices.GeneralConfiguration.SetupWizardMode == SetupWizardMode.Only)
				{
					logger.LogInformation("Shutting down due to only running setup wizard.");
					return null;
				}

				if (postSetupServices.ReloadRequired)
				{
					logger.LogInformation("TGS must restart to reload the updated configuration.");
					return null;
				}
			}

			var hostBuilder = CreateDefaultBuilder()
				.ConfigureWebHost(webHostBuilder =>
					webHostBuilder
						.UseKestrel(kestrelOptions =>
						{
							var http1Endpoints = postSetupServices.GeneralConfiguration.ApiEndPoints
								.Concat(postSetupServices.GeneralConfiguration.MetricsEndPoints)
								.Select(spec => spec.ParseIPEndPoint())
								.Distinct();

							var foundBridgeEndpoint = false;
							var bridgeEndpoint = new IPEndPoint(
								IPAddress.Loopback,
								postSetupServices.SessionConfiguration.BridgePort);
							foreach (var endPoint in http1Endpoints)
							{
								foundBridgeEndpoint |= endPoint == bridgeEndpoint;
								kestrelOptions.Listen(
									endPoint,
									listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
							}

							var swarmConfig = postSetupServices.SwarmConfiguration;
							if (swarmConfig.PrivateKey != null)
								foreach (var spec in swarmConfig.EndPoints)
									kestrelOptions.Listen(
										spec.ParseIPEndPoint(),
										listenOptions => listenOptions.Protocols = HttpProtocols.Http2);

							if (!foundBridgeEndpoint)
								kestrelOptions.Listen(
									bridgeEndpoint,
									listenOptions => listenOptions.Protocols = HttpProtocols.Http1);

							// with 515 we lost the ability to test this effectively. Just bump it slightly above the default and let the existing limit hold us back
							kestrelOptions.Limits.MaxRequestLineSize = 8400;
						})
						.UseApplication(assemblyInformationProvider, IOManager, postSetupServices)
						.SuppressStatusMessages(true)
						.UseShutdownTimeout(
							TimeSpan.FromMinutes(
								postSetupServices.GeneralConfiguration.RestartTimeoutMinutes)));

			if (updatePath != null)
				hostBuilder.UseContentRoot(
					IOManager.ResolvePath(
						IOManager.GetDirectoryName(assemblyInformationProvider.Path)));

			return new Server(hostBuilder, updatePath);
		}
#pragma warning restore CA1506
	}
}
