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
using Tgstation.Server.Host.Components.Deployment;
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
		/// The <see cref="IHttpClientFactory"/> for the <see cref="OpenDreamInstallation"/>.
		/// </summary>
		readonly IHttpClientFactory httpClientFactory;

		/// <summary>
		/// Path to the Robust.Server.dll.
		/// </summary>
		readonly string serverDllPath;

		/// <summary>
		/// Path to the DMCompiler.dll.
		/// </summary>
		readonly string compilerDllPath;

		/// <summary>
		/// Initializes a new instance of the <see cref="OpenDreamInstallation"/> class.
		/// </summary>
		/// <param name="installationIOManager">The <see cref="IIOManager"/> for the <see cref="EngineInstallationBase"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="httpClientFactory">The value of <see cref="httpClientFactory"/>.</param>
		/// <param name="dotnetPath">The path to the dotnet executable.</param>
		/// <param name="serverDllPath">The value of <see cref="serverDllPath"/>.</param>
		/// <param name="compilerDllPath">The value of <see cref="compilerDllPath"/>.</param>
		/// <param name="installationTask">The value of <see cref="InstallationTask"/>.</param>
		/// <param name="version">The value of <see cref="Version"/>.</param>
		public OpenDreamInstallation(
			IIOManager installationIOManager,
			IAsyncDelayer asyncDelayer,
			IHttpClientFactory httpClientFactory,
			string dotnetPath,
			string serverDllPath,
			string compilerDllPath,
			Task installationTask,
			EngineVersion version)
			: base(installationIOManager)
		{
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

			ServerExePath = dotnetPath ?? throw new ArgumentNullException(nameof(dotnetPath));
			CompilerExePath = dotnetPath;

			this.serverDllPath = serverDllPath ?? throw new ArgumentNullException(nameof(serverDllPath));
			this.compilerDllPath = compilerDllPath ?? throw new ArgumentNullException(nameof(compilerDllPath));
			InstallationTask = installationTask ?? throw new ArgumentNullException(nameof(installationTask));
			Version = version ?? throw new ArgumentNullException(nameof(version));

			if (version.Engine!.Value != EngineType.OpenDream)
				throw new ArgumentException($"Invalid EngineType: {version.Engine.Value}", nameof(version));
		}

		/// <inheritdoc />
		public override string FormatServerArguments(
			IDmbProvider dmbProvider,
			IReadOnlyDictionary<string, string>? parameters,
			DreamDaemonLaunchParameters launchParameters,
			string accessIdentifier,
			string? logFilePath)
		{
			ArgumentNullException.ThrowIfNull(dmbProvider);
			ArgumentNullException.ThrowIfNull(launchParameters);
			ArgumentNullException.ThrowIfNull(accessIdentifier);

			var encodedParameters = EncodeParameters(parameters, launchParameters);
			var parametersString = !String.IsNullOrEmpty(encodedParameters)
				? $" --cvar opendream.world_params=\"{encodedParameters}\""
				: String.Empty;

			var arguments = $"{serverDllPath} --cvar {(logFilePath != null ? $"log.path=\"{InstallationIOManager.GetDirectoryName(logFilePath)}\" --cvar log.format=\"{InstallationIOManager.GetFileName(logFilePath)}\"" : "log.enabled=false")} --cvar watchdog.token={accessIdentifier} --cvar log.runtimelog=false --cvar net.port={launchParameters.Port!.Value} --cvar opendream.topic_port={launchParameters.OpenDreamTopicPort!.Value}{parametersString} --cvar opendream.json_path=\"./{dmbProvider.DmbName}\"";
			return arguments;
		}

		/// <inheritdoc />
		public override string FormatCompilerArguments(string dmePath, string? additionalArguments)
		{
			if (String.IsNullOrWhiteSpace(additionalArguments))
				additionalArguments = String.Empty;
			else
				additionalArguments = $"{additionalArguments.Trim()} ";

			return $"{compilerDllPath} --suppress-unimplemented --notices-enabled {additionalArguments}\"{dmePath ?? throw new ArgumentNullException(nameof(dmePath))}\"";
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
			var timeout = asyncDelayer.Delay(TimeSpan.FromSeconds(MaximumTerminationSeconds), cancellationToken).AsTask();
			var lifetime = process.Lifetime;
			if (lifetime.IsCompleted)
				logger.LogTrace("Robust.Server already exited");

			using var httpClient = httpClientFactory.CreateClient();
			using var request = new HttpRequestMessage();
			var stopwatch = Stopwatch.StartNew();
			Task<HttpResponseMessage>? responseTask = null;
			bool responseAwaited = false;
			try
			{
				try
				{
					request.Headers.Add("WatchdogToken", accessIdentifier);
					request.RequestUri = new Uri($"http://localhost:{port}/shutdown");
					request.Content = new StringContent(
						"{\"Reason\":\"TGS session termination\"}",
						Encoding.UTF8,
						new MediaTypeHeaderValue(MediaTypeNames.Application.Json));
					request.Method = HttpMethod.Post;

					responseTask = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
					try
					{
						await Task.WhenAny(timeout, lifetime, responseTask);
						if (responseTask.IsCompleted)
						{
							responseAwaited = true;
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
			finally
			{
				if (responseTask != null && !responseAwaited)
				{
					try
					{
						await responseTask;
					}
					catch (Exception ex)
					{
						logger.LogTrace(ex, "Response task failed as expected");
					}
				}
			}
		}
	}
}
