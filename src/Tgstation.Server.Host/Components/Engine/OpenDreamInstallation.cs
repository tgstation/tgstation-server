using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Common.Http;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

#nullable disable

namespace Tgstation.Server.Host.Components.Engine
{
	/// <summary>
	/// Implementation of <see cref="IEngineInstallation"/> for <see cref="EngineType.OpenDream"/>.
	/// </summary>
	sealed class OpenDreamInstallation : EngineInstallationBase
	{
		/// <inheritdoc />
		public override EngineVersion Version { get; }

		/// <inheritdoc />
		public override string ServerExePath { get; }

		/// <inheritdoc />
		public override string CompilerExePath { get; }

		/// <inheritdoc />
		public override bool PromptsForNetworkAccess => false;

		/// <inheritdoc />
		public override bool HasStandardOutput => true;

		/// <inheritdoc />
		public override bool PreferFileLogging => true;

		/// <inheritdoc />
		public override Task InstallationTask { get; }

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="OpenDreamInstallation"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="OpenDreamInstallation"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="IAbstractHttpClientFactory"/> for the <see cref="OpenDreamInstallation"/>.
		/// </summary>
		readonly IAbstractHttpClientFactory httpClientFactory;

		/// <summary>
		/// Initializes a new instance of the <see cref="OpenDreamInstallation"/> class.
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="httpClientFactory">The value of <see cref="httpClientFactory"/>.</param>
		/// <param name="serverExePath">The value of <see cref="ServerExePath"/>.</param>
		/// <param name="compilerExePath">The value of <see cref="CompilerExePath"/>.</param>
		/// <param name="installationTask">The value of <see cref="InstallationTask"/>.</param>
		/// <param name="version">The value of <see cref="Version"/>.</param>
		public OpenDreamInstallation(
			IIOManager ioManager,
			IAsyncDelayer asyncDelayer,
			IAbstractHttpClientFactory httpClientFactory,
			string serverExePath,
			string compilerExePath,
			Task installationTask,
			EngineVersion version)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
			ServerExePath = serverExePath ?? throw new ArgumentNullException(nameof(serverExePath));
			CompilerExePath = compilerExePath ?? throw new ArgumentNullException(nameof(compilerExePath));
			InstallationTask = installationTask ?? throw new ArgumentNullException(nameof(installationTask));
			Version = version ?? throw new ArgumentNullException(nameof(version));

			if (version.Engine.Value != EngineType.OpenDream)
				throw new ArgumentException($"Invalid EngineType: {version.Engine.Value}", nameof(version));
		}

		/// <inheritdoc />
		public override string FormatServerArguments(
			IDmbProvider dmbProvider,
			IReadOnlyDictionary<string, string> parameters,
			DreamDaemonLaunchParameters launchParameters,
			string logFilePath)
		{
			ArgumentNullException.ThrowIfNull(dmbProvider);
			ArgumentNullException.ThrowIfNull(parameters);
			ArgumentNullException.ThrowIfNull(launchParameters);

			if (!parameters.TryGetValue(DMApiConstants.ParamAccessIdentifier, out var accessIdentifier))
				throw new ArgumentException($"parameters must have \"{DMApiConstants.ParamAccessIdentifier}\" set!", nameof(parameters));

			var parametersString = EncodeParameters(parameters, launchParameters);

			var loggingEnabled = logFilePath != null;
			var arguments = $"--cvar {(loggingEnabled ? $"log.path=\"{ioManager.GetDirectoryName(logFilePath)}\" --cvar log.format=\"{ioManager.GetFileName(logFilePath)}\"" : "log.enabled=false")} --cvar watchdog.token={accessIdentifier} --cvar log.runtimelog=false --cvar net.port={launchParameters.Port.Value} --cvar opendream.topic_port=0 --cvar opendream.world_params=\"{parametersString}\" --cvar opendream.json_path=\"./{dmbProvider.DmbName}\"";
			return arguments;
		}

		/// <inheritdoc />
		public override string FormatCompilerArguments(string dmePath)
			=> $"--suppress-unimplemented --notices-enabled \"{dmePath ?? throw new ArgumentNullException(nameof(dmePath))}\"";

		/// <inheritdoc />
		public override async ValueTask StopServerProcess(
			ILogger logger,
			IProcess process,
			string accessIdentifier,
			ushort port,
			CancellationToken cancellationToken)
		{
			const int MaximumTerminationSeconds = 5;

			logger.LogTrace("Attempting Robust.Server graceful exit (Timeout: {seconds}s)...", MaximumTerminationSeconds);
			var timeout = asyncDelayer.Delay(TimeSpan.FromSeconds(MaximumTerminationSeconds), cancellationToken);
			var lifetime = process.Lifetime;

			using var httpClient = httpClientFactory.CreateClient();
			using var request = new HttpRequestMessage();
			request.Headers.Add("WatchdogToken", accessIdentifier);
			request.RequestUri = new Uri($"http://localhost:{port}/shutdown");
			request.Content = new StringContent(
				"{\"Reason\":\"TGS session termination\"}",
				Encoding.UTF8,
				new MediaTypeHeaderValue(MediaTypeNames.Application.Json));
			request.Method = HttpMethod.Post;

			var responseTask = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

			await Task.WhenAny(timeout, lifetime, responseTask);
			if (responseTask.IsCompleted)
			{
				using var response = await responseTask;
				if (response.IsSuccessStatusCode)
				{
					logger.LogDebug("Robust.Server responded to the shutdown command successfully. Waiting for exit...");
					await Task.WhenAny(timeout, lifetime);
				}
			}

			if (lifetime.IsCompleted)
			{
				logger.LogTrace("Robust.Server gracefully exited");
				return;
			}

			logger.LogWarning("Robust.Server graceful exit timed out!");
			await base.StopServerProcess(logger, process, accessIdentifier, port, cancellationToken);
		}
	}
}
