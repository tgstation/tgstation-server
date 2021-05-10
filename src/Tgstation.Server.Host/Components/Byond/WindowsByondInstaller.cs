using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <summary>
	/// <see cref="IByondInstaller"/> for windows systems.
	/// </summary>
	sealed class WindowsByondInstaller : ByondInstallerBase, IDisposable
	{
		/// <summary>
		/// Directory to byond installation configuration.
		/// </summary>
		const string ByondConfigDir = "byond/cfg";

		/// <summary>
		/// BYOND's DreamDaemon config file.
		/// </summary>
		const string ByondDDConfig = "daemon.txt";

		/// <summary>
		/// Setting to add to <see cref="ByondDDConfig"/> to suppress an invisible user prompt for running a trusted mode .dmb.
		/// </summary>
		const string ByondNoPromptTrustedMode = "trusted-check 0";

		/// <summary>
		/// The directory that contains the BYOND directx redistributable.
		/// </summary>
		const string ByondDXDir = "byond/directx";

		/// <inheritdoc />
		public override string DreamDaemonName => "dreamdaemon.exe";

		/// <inheritdoc />
		public override string DreamMakerName => "dm.exe";

		/// <inheritdoc />
		public override string PathToUserByondFolder { get; }

		/// <inheritdoc />
		protected override string ByondRevisionsURLTemplate => "https://secure.byond.com/download/build/{0}/{0}.{1}_byond.zip";

		/// <summary>
		/// The <see cref="IProcessExecutor"/> for the <see cref="WindowsByondInstaller"/>.
		/// </summary>
		readonly IProcessExecutor processExecutor;

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
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="ByondInstallerBase"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ByondInstallerBase"/>.</param>
		public WindowsByondInstaller(IProcessExecutor processExecutor, IIOManager ioManager, ILogger<WindowsByondInstaller> logger)
			: base(ioManager, logger)
		{
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));

			PathToUserByondFolder = IOManager.ResolvePath(IOManager.ConcatPath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BYOND"));

			semaphore = new SemaphoreSlim(1);
			installedDirectX = false;
		}

		/// <inheritdoc />
		public void Dispose() => semaphore.Dispose();

		/// <inheritdoc />
		public override Task InstallByond(string path, Version version, CancellationToken cancellationToken)
			=> Task.WhenAll(
				SetNoPromptTrusted(path, cancellationToken),
				InstallDirectX(path, cancellationToken),
				AddDreamDaemonToFirewall(path, cancellationToken));

		/// <summary>
		/// Creates the BYOND cfg file that prevents the trusted mode dialog from appearing when launching DreamDaemon.
		/// </summary>
		/// <param name="path">The path to the BYOND installation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task SetNoPromptTrusted(string path, CancellationToken cancellationToken)
		{
			var configPath = IOManager.ConcatPath(path, ByondConfigDir);
			await IOManager.CreateDirectory(configPath, cancellationToken).ConfigureAwait(false);

			var configFilePath = IOManager.ConcatPath(configPath, ByondDDConfig);
			Logger.LogTrace("Disabling trusted prompts in {0}...", configFilePath);
			await IOManager.WriteAllBytes(
				configFilePath,
				Encoding.UTF8.GetBytes(ByondNoPromptTrustedMode),
				cancellationToken)
				.ConfigureAwait(false);
		}

		/// <summary>
		/// Attempt to install the DirectX redistributable included with BYOND.
		/// </summary>
		/// <param name="path">The path to the BYOND installation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task InstallDirectX(string path, CancellationToken cancellationToken)
		{
			using var lockContext = await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false);
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
				using var directXInstaller = processExecutor.LaunchProcess(
					IOManager.ConcatPath(rbdx, "DXSETUP.exe"),
					rbdx,
					"/silent",
					noShellExecute: true);

				int exitCode;
				using (cancellationToken.Register(() => directXInstaller.Terminate()))
					exitCode = await directXInstaller.Lifetime.ConfigureAwait(false);
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
		/// <param name="path">The path to the BYOND installation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task AddDreamDaemonToFirewall(string path, CancellationToken cancellationToken)
		{
			var dreamDaemonPath = IOManager.ResolvePath(
				IOManager.ConcatPath(
					path,
					ByondManager.BinPath,
					DreamDaemonName));

			try
			{
				using var netshProcess = processExecutor.LaunchProcess(
					"netsh.exe",
					IOManager.ResolvePath(),
					$"advfirewall firewall add rule name=\"TGS DreamDaemon\" program=\"{dreamDaemonPath}\" protocol=tcp dir=in enable=yes action=allow",
					true,
					true,
					true);

				int exitCode;
				using (cancellationToken.Register(() => netshProcess.Terminate()))
					exitCode = await netshProcess.Lifetime.ConfigureAwait(false);
				cancellationToken.ThrowIfCancellationRequested();

				Logger.LogDebug(
					"netsh.exe output:{0}{1}",
					Environment.NewLine,
					await netshProcess.GetCombinedOutput(cancellationToken).ConfigureAwait(false));

				if (exitCode != 0)
					throw new JobException(ErrorCode.ByondDreamDaemonFirewallFail, new JobException($"Invalid exit code: {exitCode}"));
			}
			catch (Exception ex)
			{
				throw new JobException(ErrorCode.ByondDreamDaemonFirewallFail, ex);
			}
		}
	}
}
