using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	sealed class DotnetDumpService : IDotnetDumpService, IDisposable
	{
		/// <summary>
		/// The <see cref="IProcessExecutor"/> for the <see cref="DotnetDumpService"/>.
		/// </summary>
		readonly IProcessExecutor processExecutor;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="DotnetDumpService"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="DotnetDumpService"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="DotnetDumpService"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="DotnetDumpService"/>.
		/// </summary>
		readonly ILogger<DotnetDumpService> logger;

		/// <summary>
		/// The <see cref="SessionConfiguration"/> for the <see cref="DotnetDumpService"/>.
		/// </summary>
		readonly SessionConfiguration sessionConfiguration;

		/// <summary>
		/// <see cref="SemaphoreSlim"/> used for checking for the presence of and installing dotnet-dump.
		/// </summary>
		readonly SemaphoreSlim installCheckSemaphore;

		/// <summary>
		/// The result of the last installation check. <see langword="true"/> means installed. <see langword="false"/> means not installed. <see langword="null"/> means the check was never run.
		/// </summary>
		bool? lastInstallCheckResult;

		/// <summary>
		/// Initializes a new instance of the <see cref="DotnetDumpService"/> class.
		/// </summary>
		/// <param name="processExecutor">The value of <see cref="processExecutor"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="sessionConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="sessionConfiguration"/>.</param>
		public DotnetDumpService(
			IProcessExecutor processExecutor,
			IIOManager ioManager,
			IAssemblyInformationProvider assemblyInformationProvider,
			IPlatformIdentifier platformIdentifier,
			ILogger<DotnetDumpService> logger,
			IOptions<SessionConfiguration> sessionConfigurationOptions)
		{
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			sessionConfiguration = sessionConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(sessionConfigurationOptions));

			installCheckSemaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose() => installCheckSemaphore.Dispose();

		/// <inheritdoc />
		public async ValueTask EnsureInstalled(bool deploymentPipeline, CancellationToken cancellationToken)
		{
			logger.LogTrace("EnsureInstalled");

			if (lastInstallCheckResult == true)
				return;

			using (await SemaphoreSlimContext.Lock(installCheckSemaphore, cancellationToken))
			{
				var installDir = await CheckInstalled(cancellationToken);
				if (lastInstallCheckResult == true)
					return;

				await Install(installDir ?? GetDirectoryPath(), deploymentPipeline, cancellationToken);
			}
		}

		/// <inheritdoc />
		public async ValueTask<bool> Dump(IProcess process, string outputFile, CancellationToken cancellationToken)
		{
			logger.LogTrace("dotnet-dump requested...");
			string? installDir = null;
			if (!lastInstallCheckResult.HasValue)
				using (await SemaphoreSlimContext.Lock(installCheckSemaphore, cancellationToken))
					installDir = await CheckInstalled(cancellationToken);

			if (lastInstallCheckResult != true)
				return false;

			installDir ??= GetDirectoryPath();
			var exeExtension = platformIdentifier.IsWindows
				? ".exe"
				: String.Empty;

			var resolvedInstallDir = ioManager.ResolvePath(installDir);

			var executablePath = ioManager.ConcatPath(
				resolvedInstallDir,
				$"dotnet-dump{exeExtension}");

			await using var dumpProcess = processExecutor.LaunchProcess(
				executablePath,
				resolvedInstallDir,
				$"collect -p {process.Id} -o \"{outputFile}\"",
				readStandardHandles: true,
				noShellExecute: true);

			int? exitCode;
			using (cancellationToken.Register(() => dumpProcess.Terminate()))
				exitCode = await dumpProcess.Lifetime;

			var output = await dumpProcess.GetCombinedOutput(cancellationToken);

			if (exitCode != 0)
				throw new JobException(
					ErrorCode.DumpProcessFailure,
					new JobException(
						$"Exit Code: {exitCode}{Environment.NewLine}Output:{Environment.NewLine}{output}"));

			logger.LogDebug("dotnet-dump output:{newline}{output}", Environment.NewLine, output);

			return true;
		}

		/// <summary>
		/// Sets <see cref="lastInstallCheckResult"/> if it is <see langword="null"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns><see langword="null"/> if <see cref="lastInstallCheckResult"/> was not <see langword="null"/>. The result of <see cref="GetDirectoryPath"/> otherwise.</returns>
		async ValueTask<string?> CheckInstalled(CancellationToken cancellationToken)
		{
			if (lastInstallCheckResult.HasValue)
				return null;

			logger.LogTrace("Checking if dotnet-dump is installed...");

			var directory = GetDirectoryPath();
			lastInstallCheckResult = await ioManager.DirectoryExists(directory, cancellationToken);

			logger.LogTrace("dotnet-dump installed: {result}", lastInstallCheckResult.Value);

			return directory;
		}

		/// <summary>
		/// Locally install the dotnet-dump tool.
		/// </summary>
		/// <param name="installDir">The directory to install dotnet dump in.</param>
		/// <param name="deploymentPipeline">If this operation is part of the deployment pipeline.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask Install(string installDir, bool deploymentPipeline, CancellationToken cancellationToken)
		{
			var dotnetPath = await DotnetHelper.GetDotnetPath(platformIdentifier, ioManager, cancellationToken);

			logger.LogTrace("Ensuring installation directory is gone...");
			await ioManager.DeleteDirectory(installDir, cancellationToken);

			var resolvedInstallDir = ioManager.ResolvePath(installDir);

			logger.LogTrace("Installing dotnet-dump...");
			await using var installProcess = processExecutor.LaunchProcess(
				dotnetPath,
				ioManager.ResolvePath(),
				$"tool install --tool-path \"{resolvedInstallDir}\" dotnet-dump",
				readStandardHandles: true,
				noShellExecute: true);

			if (deploymentPipeline && sessionConfiguration.LowPriorityDeploymentProcesses)
				installProcess.AdjustPriority(false);

			int? exitCode;
			using (cancellationToken.Register(() => installProcess.Terminate()))
				exitCode = await installProcess.Lifetime;

			var output = await installProcess.GetCombinedOutput(cancellationToken);

			if (exitCode != 0)
				throw new JobException(
					ErrorCode.CantInstallDotnetDump,
					new JobException(
						$"Exit Code: {exitCode}{Environment.NewLine}Output:{Environment.NewLine}{output}"));

			logger.LogDebug("dotnet tool install output:{newline}{output}", Environment.NewLine, output);
		}

		/// <summary>
		/// Get the path to the dotnet-dump installation directory TGS uses.
		/// </summary>
		/// <returns>The path to the dotnet-dump installation directory.</returns>
		string GetDirectoryPath() => ioManager.ConcatPath(
			ioManager.GetPathInLocalDirectory(assemblyInformationProvider),
			"dotnet-dump");
	}
}
