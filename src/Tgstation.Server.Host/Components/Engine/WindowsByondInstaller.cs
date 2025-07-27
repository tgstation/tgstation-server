using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <summary>
	/// <see cref="IEngineInstaller"/> for windows systems.
	/// </summary>
	sealed class WindowsByondInstaller : ByondInstallerBase, IDisposable
	{
		/// <summary>
		/// Directory to byond installation configuration.
		/// </summary>
		const string ByondConfigDirectory = "byond/cfg";

		/// <summary>
		/// BYOND's DreamDaemon config file.
		/// </summary>
		const string ByondDreamDaemonConfigFilename = "daemon.txt";

		/// <summary>
		/// Setting to add to <see cref="ByondDreamDaemonConfigFilename"/> to suppress an invisible user prompt for running a trusted mode .dmb.
		/// </summary>
		const string ByondNoPromptTrustedMode = "trusted-check 0";

		/// <summary>
		/// The directory that contains the BYOND directx redistributable.
		/// </summary>
		const string ByondDXDir = "byond/directx";

		/// <summary>
		/// The file TGS uses to determine if dd.exe has been firewalled.
		/// </summary>
		const string TgsFirewalledDDFile = "TGSFirewalledDD";

		/// <summary>
		/// The name of the list of trusted .dmb files in the user's BYOND cfg directory.
		/// </summary>
		const string TrustedDmbFileName = "trusted.txt";

		/// <summary>
		/// <see cref="SemaphoreSlim"/> for writing to files in the user's BYOND directory.
		/// </summary>
		static readonly SemaphoreSlim UserFilesSemaphore = new(1, 1);

		/// <summary>
		/// The first version of BYOND to ship with dd.exe on the Windows build.
		/// </summary>
		public static Version DDExeVersion => new(515, 1598);

		/// <inheritdoc />
		protected override string DreamMakerName => "dm.exe";

		/// <inheritdoc />
		protected override string PathToUserFolder { get; }

		/// <inheritdoc />
		protected override string OSMarkerTemplate => "Windows";

		/// <summary>
		/// The <see cref="IProcessExecutor"/> for the <see cref="WindowsByondInstaller"/>.
		/// </summary>
		readonly IProcessExecutor processExecutor;

		/// <summary>
		/// The <see cref="SessionConfiguration"/> for the <see cref="WindowsByondInstaller"/>.
		/// </summary>
		readonly SessionConfiguration sessionConfiguration;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> for the <see cref="WindowsByondInstaller"/>.
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// If DirectX was installed.
		/// </summary>
		bool installedDirectX;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowsByondInstaller"/> class.
		/// </summary>
		/// <param name="processExecutor">The value of <see cref="processExecutor"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptionsMonitor{TOptions}"/> containing the <see cref="GeneralConfiguration"/>.</param>
		/// <param name="sessionConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="sessionConfiguration"/>.</param>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="ByondInstallerBase"/>.</param>
		/// <param name="fileDownloader">The <see cref="IFileDownloader"/> for the <see cref="ByondInstallerBase"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ByondInstallerBase"/>.</param>
		public WindowsByondInstaller(
			IProcessExecutor processExecutor,
			IIOManager ioManager,
			IFileDownloader fileDownloader,
			IOptionsMonitor<GeneralConfiguration> generalConfigurationOptions,
			IOptions<SessionConfiguration> sessionConfigurationOptions,
			ILogger<WindowsByondInstaller> logger)
			: base(ioManager, logger, fileDownloader, generalConfigurationOptions)
		{
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			sessionConfiguration = sessionConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(sessionConfigurationOptions));

			var useServiceSpecialTactics = Environment.Is64BitProcess && Environment.UserName == $"{Environment.MachineName}$";

			var documentsDirectory = useServiceSpecialTactics
				? Environment.ExpandEnvironmentVariables("%SystemRoot%\\SysWOW64\\config\\systemprofile\\Documents")
				: Environment.GetFolderPath(
					Environment.SpecialFolder.MyDocuments,
					Environment.SpecialFolderOption.DoNotVerify);

			PathToUserFolder = IOManager.ResolvePath(
				IOManager.ConcatPath(documentsDirectory, "BYOND"));

			semaphore = new SemaphoreSlim(1);
			installedDirectX = false;
		}

		/// <inheritdoc />
		public void Dispose() => semaphore.Dispose();

		/// <inheritdoc />
		public override ValueTask Install(EngineVersion version, string path, bool deploymentPipelineProcesses, CancellationToken cancellationToken)
		{
			CheckVersionValidity(version);
			ArgumentNullException.ThrowIfNull(path);

			var noPromptTrustedTask = SetNoPromptTrusted(path, cancellationToken);
			var installDirectXTask = InstallDirectX(path, cancellationToken);
			var tasks = new List<ValueTask>(3)
			{
				noPromptTrustedTask,
				installDirectXTask,
			};

			if (!GeneralConfigurationOptions.CurrentValue.SkipAddingByondFirewallException)
			{
				var firewallTask = AddDreamDaemonToFirewall(version, path, deploymentPipelineProcesses, cancellationToken);
				tasks.Add(firewallTask);
			}

			return ValueTaskExtensions.WhenAll(tasks);
		}

		/// <inheritdoc />
		public override async ValueTask UpgradeInstallation(EngineVersion version, string path, CancellationToken cancellationToken)
		{
			CheckVersionValidity(version);
			ArgumentNullException.ThrowIfNull(path);

			if (GeneralConfigurationOptions.CurrentValue.SkipAddingByondFirewallException)
				return;

			if (version.Version < DDExeVersion)
				return;

			if (await IOManager.FileExists(IOManager.ConcatPath(path, TgsFirewalledDDFile), cancellationToken))
				return;

			Logger.LogInformation("BYOND Version {version} needs dd.exe added to firewall", version);
			await AddDreamDaemonToFirewall(version, path, true, cancellationToken);
		}

		/// <inheritdoc />
		public override async ValueTask TrustDmbPath(EngineVersion version, string fullDmbPath, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(version);
			ArgumentNullException.ThrowIfNull(fullDmbPath);

			var byondDir = PathToUserFolder;
			var cfgDir = IOManager.ConcatPath(
				byondDir,
				CfgDirectoryName);
			var trustedFilePath = IOManager.ConcatPath(
				cfgDir,
				TrustedDmbFileName);

			Logger.LogDebug("Adding .dmb ({dmbPath}) to {trustedFilePath}", fullDmbPath, trustedFilePath);

			using (await SemaphoreSlimContext.Lock(UserFilesSemaphore, cancellationToken))
			{
				string trustedFileText;
				var filePreviouslyExisted = await IOManager.FileExists(trustedFilePath, cancellationToken);
				if (filePreviouslyExisted)
				{
					var trustedFileBytes = await IOManager.ReadAllBytes(trustedFilePath, cancellationToken);
					trustedFileText = Encoding.UTF8.GetString(trustedFileBytes.Span);
					trustedFileText = $"{trustedFileText.Trim()}{Environment.NewLine}";
				}
				else
					trustedFileText = String.Empty;

				if (trustedFileText.Contains(fullDmbPath, StringComparison.Ordinal))
					return;

				trustedFileText = $"{trustedFileText}{fullDmbPath}{Environment.NewLine}";

				var newTrustedFileBytes = Encoding.UTF8.GetBytes(trustedFileText);

				if (!filePreviouslyExisted)
					await IOManager.CreateDirectory(cfgDir, cancellationToken);

				await IOManager.WriteAllBytes(trustedFilePath, newTrustedFileBytes, cancellationToken);
			}
		}

		/// <inheritdoc />
		protected override string GetDreamDaemonName(Version byondVersion, out bool supportsCli)
		{
			supportsCli = byondVersion >= DDExeVersion && !sessionConfiguration.ForceUseDreamDaemonExe;
			return supportsCli ? "dd.exe" : "dreamdaemon.exe";
		}

		/// <inheritdoc />
		protected override IEnumerable<string> AdditionalCacheCleanFilePaths(string configDirectory)
		{
			// Delete trusted.txt so it doesn't grow too large
			var trustedFilePath =
				IOManager.ConcatPath(
					configDirectory,
					TrustedDmbFileName);

			Logger.LogTrace("Deleting trusted .dmbs file {trustedFilePath}", trustedFilePath);
			yield return trustedFilePath;
		}

		/// <summary>
		/// Creates the BYOND cfg file that prevents the trusted mode dialog from appearing when launching DreamDaemon.
		/// </summary>
		/// <param name="path">The path to the BYOND installation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async ValueTask SetNoPromptTrusted(string path, CancellationToken cancellationToken)
		{
			var configPath = IOManager.ConcatPath(path, ByondConfigDirectory);
			await IOManager.CreateDirectory(configPath, cancellationToken);

			var configFilePath = IOManager.ConcatPath(configPath, ByondDreamDaemonConfigFilename);
			Logger.LogTrace("Disabling trusted prompts in {configFilePath}...", configFilePath);
			await IOManager.WriteAllBytes(
				configFilePath,
				Encoding.UTF8.GetBytes(ByondNoPromptTrustedMode),
				cancellationToken);
		}

		/// <summary>
		/// Attempt to install the DirectX redistributable included with BYOND.
		/// </summary>
		/// <param name="path">The path to the BYOND installation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask InstallDirectX(string path, CancellationToken cancellationToken)
		{
			using var lockContext = await SemaphoreSlimContext.Lock(semaphore, cancellationToken);
			if (installedDirectX)
			{
				Logger.LogTrace("DirectX already installed.");
				return;
			}

			Logger.LogTrace("Installing DirectX redistributable...");

			// always install it, it's pretty fast and will do better redundancy checking than us
			var rbdx = IOManager.ConcatPath(path, ByondDXDir);

			try
			{
				// noShellExecute because we aren't doing runas shennanigans
				await using var directXInstaller = await processExecutor.LaunchProcess(
					IOManager.ConcatPath(rbdx, "DXSETUP.exe"),
					rbdx,
					"/silent",
					cancellationToken,
					noShellExecute: true);

				int exitCode;
				using (cancellationToken.Register(() => directXInstaller.Terminate()))
					exitCode = (await directXInstaller.Lifetime).Value;
				cancellationToken.ThrowIfCancellationRequested();

				if (exitCode != 0)
					throw new JobException(ErrorCode.ByondDirectXInstallFail, new JobException($"Invalid exit code: {exitCode}"));
				installedDirectX = true;
			}
			catch (Exception e)
			{
				throw new JobException(ErrorCode.ByondDirectXInstallFail, e);
			}
		}

		/// <summary>
		/// Attempt to add the DreamDaemon executable as an exception to the Windows firewall.
		/// </summary>
		/// <param name="version">The BYOND <see cref="EngineVersion"/>.</param>
		/// <param name="path">The path to the BYOND installation.</param>
		/// <param name="deploymentPipelineProcesses">If the operation is part of the deployment pipeline.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask AddDreamDaemonToFirewall(EngineVersion version, string path, bool deploymentPipelineProcesses, CancellationToken cancellationToken)
		{
			var dreamDaemonName = GetDreamDaemonName(version.Version!, out var usesDDExe);

			var dreamDaemonPath = IOManager.ResolvePath(
				IOManager.ConcatPath(
					path,
					ByondBinPath,
					dreamDaemonName));

			int exitCode;
			try
			{
				// I really wish we could add the instance name here but
				// 1. It'd make IByondInstaller need to be transient per-instance and WindowsByondInstaller relys on being a singleton for its DX installer call
				// 2. The instance could be renamed, so it'd have to be an unfriendly ID anyway.
				var ruleName = $"TGS DreamDaemon {version}";

				exitCode = await WindowsFirewallHelper.AddFirewallException(
					processExecutor,
					Logger,
					ruleName,
					dreamDaemonPath,
					deploymentPipelineProcesses && sessionConfiguration.LowPriorityDeploymentProcesses,
					cancellationToken);
			}
			catch (Exception ex)
			{
				throw new JobException(ErrorCode.EngineFirewallFail, ex);
			}

			if (exitCode != 0)
				throw new JobException(ErrorCode.EngineFirewallFail, new JobException($"Invalid exit code: {exitCode}"));

			if (usesDDExe)
				await IOManager.WriteAllBytes(
					IOManager.ConcatPath(path, TgsFirewalledDDFile),
					Array.Empty<byte>(),
					cancellationToken);
		}
	}
}
