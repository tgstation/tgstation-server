using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <summary>
	/// <see cref="IByondInstaller"/> for Posix systems.
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
		public override string DreamDaemonName => DreamDaemonExecutableName + ShellScriptExtension;

		/// <inheritdoc />
		public override string DreamMakerName => DreamMakerExecutableName + ShellScriptExtension;

		/// <inheritdoc />
		public override string PathToUserByondFolder { get; }

		/// <inheritdoc />
		protected override string ByondRevisionsURLTemplate => "https://secure.byond.com/download/build/{0}/{0}.{1}_byond_linux.zip";

		/// <summary>
		/// The <see cref="IPostWriteHandler"/> for the <see cref="PosixByondInstaller"/>.
		/// </summary>
		readonly IPostWriteHandler postWriteHandler;

		/// <summary>
		/// Initializes a new instance of the <see cref="PosixByondInstaller"/> class.
		/// </summary>
		/// <param name="postWriteHandler">The value of <see cref="postWriteHandler"/>.</param>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="ByondInstallerBase"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="ByondInstallerBase"/>.</param>
		public PosixByondInstaller(IPostWriteHandler postWriteHandler, IIOManager ioManager, ILogger<PosixByondInstaller> logger)
			: base(ioManager, logger)
		{
			this.postWriteHandler = postWriteHandler ?? throw new ArgumentNullException(nameof(postWriteHandler));

			PathToUserByondFolder = IOManager.ResolvePath(
				IOManager.ConcatPath(
					Environment.GetFolderPath(
						Environment.SpecialFolder.UserProfile),
					"./byond/cache"));
		}

		/// <inheritdoc />
		public override Task InstallByond(string path, Version version, CancellationToken cancellationToken)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			// write the scripts for running the ting
			// need to add $ORIGIN to LD_LIBRARY_PATH
			const string StandardScript = "#!/bin/sh\nexport LD_LIBRARY_PATH=\"\\$ORIGIN:$LD_LIBRARY_PATH\"\nBASEDIR=$(dirname \"$0\")\nexec \"$BASEDIR/{0}\" \"$@\"\n";

			var dreamDaemonScript = String.Format(CultureInfo.InvariantCulture, StandardScript, DreamDaemonExecutableName);
			var dreamMakerScript = String.Format(CultureInfo.InvariantCulture, StandardScript, DreamMakerExecutableName);

			async Task WriteAndMakeExecutable(string pathToScript, string script)
			{
				Logger.LogTrace("Writing script {0}:{1}{2}", pathToScript, Environment.NewLine, script);
				await IOManager.WriteAllBytes(pathToScript, Encoding.ASCII.GetBytes(script), cancellationToken).ConfigureAwait(false);
				postWriteHandler.HandleWrite(IOManager.ResolvePath(pathToScript));
			}

			var basePath = IOManager.ConcatPath(path, ByondManager.BinPath);

			var task = Task.WhenAll(WriteAndMakeExecutable(IOManager.ConcatPath(basePath, DreamDaemonName), dreamDaemonScript), WriteAndMakeExecutable(IOManager.ConcatPath(basePath, DreamMakerName), dreamMakerScript));

			postWriteHandler.HandleWrite(IOManager.ConcatPath(basePath, DreamDaemonExecutableName));
			postWriteHandler.HandleWrite(IOManager.ConcatPath(basePath, DreamMakerExecutableName));

			return task;
		}
	}
}
