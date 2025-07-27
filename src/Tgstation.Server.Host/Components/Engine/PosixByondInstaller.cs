using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <summary>
	/// <see cref="IEngineInstaller"/> for Posix systems.
	/// </summary>
	sealed class PosixByondInstaller : ByondInstallerBase
	{
		/// <summary>
		/// The name of the DreamDaemon binary file.
		/// </summary>
		const string DreamDaemonExecutableName = "DreamDaemon";

		/// <summary>
		/// The name of the DreamMaker binary file.
		/// </summary>
		const string DreamMakerExecutableName = "DreamMaker";

		/// <summary>
		/// File extension for shell scripts.
		/// </summary>
		const string ShellScriptExtension = ".sh";

		/// <inheritdoc />
		protected override string PathToUserFolder { get; }

		/// <inheritdoc />
		protected override string DreamMakerName => DreamMakerExecutableName + ShellScriptExtension;

		/// <inheritdoc />
		protected override string OSMarkerTemplate => "Linux";

		/// <summary>
		/// The <see cref="IPostWriteHandler"/> for the <see cref="PosixByondInstaller"/>.
		/// </summary>
		readonly IPostWriteHandler postWriteHandler;

		/// <summary>
		/// Initializes a new instance of the <see cref="PosixByondInstaller"/> class.
		/// </summary>
		/// <param name="postWriteHandler">The value of <see cref="postWriteHandler"/>.</param>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="ByondInstallerBase"/>.</param>
		/// <param name="fileDownloader">The <see cref="IFileDownloader"/> for the <see cref="ByondInstallerBase"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="GeneralConfiguration"/> <see cref="IOptionsMonitor{TOptions}"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ByondInstallerBase"/>.</param>
		public PosixByondInstaller(
			IPostWriteHandler postWriteHandler,
			IIOManager ioManager,
			IFileDownloader fileDownloader,
			IOptionsMonitor<GeneralConfiguration> generalConfigurationOptions,
			ILogger<PosixByondInstaller> logger)
			: base(ioManager, logger, fileDownloader, generalConfigurationOptions)
		{
			this.postWriteHandler = postWriteHandler ?? throw new ArgumentNullException(nameof(postWriteHandler));

			PathToUserFolder = IOManager.ResolvePath(
				IOManager.ConcatPath(
					Environment.GetFolderPath(
						Environment.SpecialFolder.UserProfile,
						Environment.SpecialFolderOption.DoNotVerify),
					"./.byond/cache"));
		}

		/// <inheritdoc />
		public override ValueTask Install(EngineVersion version, string path, bool deploymentPipelineProcesses, CancellationToken cancellationToken)
		{
			CheckVersionValidity(version);
			ArgumentNullException.ThrowIfNull(path);

			// write the scripts for running the ting
			// need to add $ORIGIN to LD_LIBRARY_PATH
			const string StandardScript = "#!/bin/sh\nexport LD_LIBRARY_PATH=\"\\$ORIGIN:$LD_LIBRARY_PATH\"\nBASEDIR=$(dirname \"$0\")\nexec \"$BASEDIR/{0}\" \"$@\"\n";

			var dreamDaemonScript = String.Format(CultureInfo.InvariantCulture, StandardScript, DreamDaemonExecutableName);
			var dreamMakerScript = String.Format(CultureInfo.InvariantCulture, StandardScript, DreamMakerExecutableName);

			async ValueTask WriteAndMakeExecutable(string pathToScript, string script)
			{
				Logger.LogTrace("Writing script {path}:{newLine}{scriptContents}", pathToScript, Environment.NewLine, script);
				await IOManager.WriteAllBytes(pathToScript, Encoding.ASCII.GetBytes(script), cancellationToken);
				postWriteHandler.HandleWrite(IOManager.ResolvePath(pathToScript));
			}

			var basePath = IOManager.ConcatPath(path, ByondBinPath);

			var ddTask = WriteAndMakeExecutable(
				IOManager.ConcatPath(basePath, GetDreamDaemonName(version.Version!, out _)),
				dreamDaemonScript);

			var dmTask = WriteAndMakeExecutable(
				IOManager.ConcatPath(basePath, DreamMakerName),
				dreamMakerScript);

			var task = ValueTaskExtensions.WhenAll(
				ddTask,
				dmTask);

			postWriteHandler.HandleWrite(IOManager.ConcatPath(basePath, DreamDaemonExecutableName));
			postWriteHandler.HandleWrite(IOManager.ConcatPath(basePath, DreamMakerExecutableName));

			return task;
		}

		/// <inheritdoc />
		public override ValueTask UpgradeInstallation(EngineVersion version, string path, CancellationToken cancellationToken)
		{
			CheckVersionValidity(version);
			ArgumentNullException.ThrowIfNull(path);

			return ValueTask.CompletedTask;
		}

		/// <inheritdoc />
		public override ValueTask TrustDmbPath(EngineVersion version, string fullDmbPath, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(version);
			ArgumentNullException.ThrowIfNull(fullDmbPath);

			Logger.LogTrace("No need to trust .dmb path \"{path}\" on POSIX", fullDmbPath);
			return ValueTask.CompletedTask;
		}

		/// <inheritdoc />
		protected override string GetDreamDaemonName(Version byondVersion, out bool supportsCli)
		{
			supportsCli = true;
			return DreamDaemonExecutableName + ShellScriptExtension;
		}
	}
}
