using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <summary>
	/// <see cref="IByondInstaller"/> for windows systems
	/// </summary>
	sealed class WindowsByondInstaller : ByondInstallerBase, IDisposable
	{
		/// <summary>
		/// Directory to byond installation configuration
		/// </summary>
		const string ByondConfigDir = "byond/cfg";

		/// <summary>
		/// BYOND's DreamDaemon config file
		/// </summary>
		const string ByondDDConfig = "daemon.txt";

		/// <summary>
		/// Setting to add to <see cref="ByondDDConfig"/> to suppress an invisible user prompt for running a trusted mode .dmb
		/// </summary>
		const string ByondNoPromptTrustedMode = "trusted-check 0";

		/// <summary>
		/// The directory that contains the BYOND directx redistributable
		/// </summary>
		const string ByondDXDir = "byond/directx";

		/// <inheritdoc />
		public override string DreamDaemonName => "dreamdaemon.exe";

		/// <inheritdoc />
		public override string DreamMakerName => "dm.exe";

		/// <inheritdoc />
		protected override string ByondRevisionsURLTemplate => "https://secure.byond.com/download/build/{0}/{0}.{1}_byond.zip";

		/// <summary>
		/// The <see cref="IProcessExecutor"/> for the <see cref="WindowsByondInstaller"/>
		/// </summary>
		readonly IProcessExecutor processExecutor;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> for the <see cref="WindowsByondInstaller"/>
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// If DirectX was installed
		/// </summary>
		bool installedDirectX;

		/// <summary>
		/// Construct a <see cref="WindowsByondInstaller"/>
		/// </summary>
		/// <param name="processExecutor">The value of <see cref="processExecutor"/></param>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="ByondInstallerBase"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ByondInstallerBase"/>.</param>
		public WindowsByondInstaller(IProcessExecutor processExecutor, IIOManager ioManager, ILogger<WindowsByondInstaller> logger)
			: base(ioManager, logger)
		{
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));

			semaphore = new SemaphoreSlim(1);
			installedDirectX = false;
		}

		/// <inheritdoc />
		public void Dispose() => semaphore.Dispose();

		/// <inheritdoc />
		public override async Task CleanCache(CancellationToken cancellationToken)
		{
			try
			{
				await IOManager.DeleteDirectory(IOManager.ConcatPath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "byond/cache"), cancellationToken).ConfigureAwait(false);
			}
			catch(OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				Logger.LogWarning("Error deleting BYOND cache! Exception: {0}", e);
			}
		}

		/// <inheritdoc />
		public override async Task InstallByond(string path, Version version, CancellationToken cancellationToken)
		{
			async Task SetNoPromptTrusted()
			{
				var configPath = IOManager.ConcatPath(path, ByondConfigDir);
				await IOManager.CreateDirectory(configPath, cancellationToken).ConfigureAwait(false);
				await IOManager.WriteAllBytes(IOManager.ConcatPath(configPath, ByondDDConfig), Encoding.UTF8.GetBytes(ByondNoPromptTrustedMode), cancellationToken).ConfigureAwait(false);
			}

			var setNoPromptTrustedModeTask = SetNoPromptTrusted();

			// after this version lummox made DD depend of directx lol
			// but then he became amazing and not only fixed it but also gave us 30s compiles \[T]/
			// then he readded it again so -_-
			if (!installedDirectX)
				using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
					if (!installedDirectX)
					{
						// ^check again because race conditions
						// always install it, it's pretty fast and will do better redundancy checking than us
						var rbdx = IOManager.ConcatPath(path, ByondDXDir);

						// noShellExecute because we aren't doing runas shennanigans
						IProcess directXInstaller;
						try
						{
							directXInstaller = processExecutor.LaunchProcess(IOManager.ConcatPath(rbdx, "DXSETUP.exe"), rbdx, "/silent", noShellExecute: true);
						}
						catch (Exception e)
						{
							throw new JobException("Unable to start DirectX installer process! Is the server running with admin privileges?", e);
						}

						using (directXInstaller)
						{
							int exitCode;
							using (cancellationToken.Register(() => directXInstaller.Terminate()))
								exitCode = await directXInstaller.Lifetime.ConfigureAwait(false);
							cancellationToken.ThrowIfCancellationRequested();

							if (exitCode != 0)
								throw new JobException(String.Format(CultureInfo.InvariantCulture, "Failed to install included DirectX! Exit code: {0}", exitCode));
							installedDirectX = true;
						}
					}

			await setNoPromptTrustedModeTask.ConfigureAwait(false);
		}
	}
}
