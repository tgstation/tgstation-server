using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		public override bool UseDotnetDump => true;

		/// <inheritdoc />
		public override Task InstallationTask { get; }

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
		/// <param name="installationIOManager">The <see cref="IIOManager"/> for the <see cref="EngineInstallationBase"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="httpClientFactory">The value of <see cref="httpClientFactory"/>.</param>
		/// <param name="serverExePath">The value of <see cref="ServerExePath"/>.</param>
		/// <param name="compilerExePath">The value of <see cref="CompilerExePath"/>.</param>
		/// <param name="installationTask">The value of <see cref="InstallationTask"/>.</param>
		/// <param name="version">The value of <see cref="Version"/>.</param>
		public OpenDreamInstallation(
			IIOManager installationIOManager,
			IAsyncDelayer asyncDelayer,
			IAbstractHttpClientFactory httpClientFactory,
			string serverExePath,
			string compilerExePath,
			Task installationTask,
			EngineVersion version)
			: base(installationIOManager)
		{
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
			ServerExePath = serverExePath ?? throw new ArgumentNullException(nameof(serverExePath));
			CompilerExePath = compilerExePath ?? throw new ArgumentNullException(nameof(compilerExePath));
			InstallationTask = installationTask ?? throw new ArgumentNullException(nameof(installationTask));
			Version = version ?? throw new ArgumentNullException(nameof(version));

			if (version.Engine!.Value != EngineType.OpenDream)
				throw new ArgumentException($"Invalid EngineType: {version.Engine.Value}", nameof(version));
		}

		/// <inheritdoc />
		public override string FormatServerArguments(
			IDmbProvider dmbProvider,
			IReadOnlyDictionary<string, string> parameters,
			DreamDaemonLaunchParameters launchParameters,
			string? logFilePath)
		{
			ArgumentNullException.ThrowIfNull(dmbProvider);
			ArgumentNullException.ThrowIfNull(parameters);
			ArgumentNullException.ThrowIfNull(launchParameters);

			if (!parameters.TryGetValue(DMApiConstants.ParamAccessIdentifier, out var accessIdentifier))
				throw new ArgumentException($"parameters must have \"{DMApiConstants.ParamAccessIdentifier}\" set!", nameof(parameters));

			var parametersString = EncodeParameters(parameters, launchParameters);

			var arguments = $"--cvar {(logFilePath != null ? $"log.path=\"{InstallationIOManager.GetDirectoryName(logFilePath)}\" --cvar log.format=\"{InstallationIOManager.GetFileName(logFilePath)}\"" : "log.enabled=false")} --cvar watchdog.token={accessIdentifier} --cvar log.runtimelog=false --cvar net.port={launchParameters.Port!.Value} --cvar opendream.topic_port=0 --cvar opendream.world_params=\"{parametersString}\" --cvar opendream.json_path=\"./{dmbProvider.DmbName}\"";
			return arguments;
		}

		/// <inheritdoc />
		public override string FormatCompilerArguments(string dmePath, string? additionalArguments)
		{
			if (String.IsNullOrWhiteSpace(additionalArguments))
				additionalArguments = String.Empty;
			else
				additionalArguments = $"{additionalArguments.Trim()} ";

			return $"--suppress-unimplemented --notices-enabled {additionalArguments}\"{dmePath ?? throw new ArgumentNullException(nameof(dmePath))}\"";
		}

		/// <inheritdoc />
		public override async ValueTask StopServerProcess(
			ILogger logger,
			IProcess process,
			string accessIdentifier,
			ushort port,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(logger);

			const int MaximumTerminationSeconds = 5;

			logger.LogTrace("Attempting Robust.Server graceful exit (Timeout: {seconds}s)...", MaximumTerminationSeconds);
			var timeout = asyncDelayer.Delay(TimeSpan.FromSeconds(MaximumTerminationSeconds), cancellationToken);
			var lifetime = process.Lifetime;
			if (lifetime.IsCompleted)
				logger.LogTrace("Robust.Server already exited");

			var stopwatch = Stopwatch.StartNew();
			try
			{
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
				try
				{
					await Task.WhenAny(timeout, lifetime, responseTask);
					if (responseTask.IsCompleted)
					{
						using var response = await responseTask;
						if (response.IsSuccessStatusCode)
						{
							logger.LogDebug("Robust.Server responded to the shutdown command successfully ({requestMs}ms). Waiting for exit...", stopwatch.ElapsedMilliseconds);
							await Task.WhenAny(timeout, lifetime);
						}
					}

					if (!lifetime.IsCompleted)
						logger.LogWarning("Robust.Server graceful exit timed out!");
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					logger.LogDebug(ex, "Unable to send graceful exit request to Robust.Server watchdog API!");
				}

				if (lifetime.IsCompleted)
				{
					logger.LogTrace("Robust.Server exited without termination");
					return;
				}
			}
			finally
			{
				logger.LogTrace("Robust.Server graceful shutdown attempt took {totalMs}ms", stopwatch.ElapsedMilliseconds);
			}

			await base.StopServerProcess(logger, process, accessIdentifier, port, cancellationToken);
		}
	}
}
