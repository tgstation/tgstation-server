using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Common;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <summary>
	/// Implementation of <see cref="IEngineInstaller"/> for <see cref="EngineType.OpenDream"/>.
	/// </summary>
	sealed class OpenDreamInstaller : EngineInstallerBase
	{
		/// <summary>
		/// The name of the subdirectory used to store the server and compiler binaries.
		/// </summary>
		const string InstallationBinDirectory = "bin";

		/// <summary>
		/// The name of the subdirectory used for the <see cref="RepositoryEngineInstallationData"/>'s copy.
		/// </summary>
		const string InstallationSourceSubDirectory = "TgsSourceSubdir";

		/// <inheritdoc />
		protected override EngineType TargetEngineType => EngineType.OpenDream;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="OpenDreamInstaller"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="IProcessExecutor"/> for the <see cref="OpenDreamInstaller"/>.
		/// </summary>
		readonly IProcessExecutor processExecutor;

		/// <summary>
		/// The <see cref="IRepositoryManager"/> for the OpenDream repository.
		/// </summary>
		readonly IRepositoryManager repositoryManager;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="OpenDreamInstaller"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="OpenDreamInstaller"/> class.
		/// </summary>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="EngineInstallerBase"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="EngineInstallerBase"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="processExecutor">The value of <see cref="processExecutor"/>.</param>
		/// <param name="repositoryManager">The value of <see cref="repositoryManager"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="generalConfiguration"/>.</param>
		public OpenDreamInstaller(
			IIOManager ioManager,
			ILogger<OpenDreamInstaller> logger,
			IPlatformIdentifier platformIdentifier,
			IProcessExecutor processExecutor,
			IRepositoryManager repositoryManager,
			IOptions<GeneralConfiguration> generalConfigurationOptions)
			: base(ioManager, logger)
		{
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			this.repositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <inheritdoc />
		public override Task CleanCache(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public override IEngineInstallation CreateInstallation(ByondVersion version, string path, Task installationTask)
		{
			CheckVersionValidity(version);
			var binPathForVersion = IOManager.ConcatPath(path, InstallationBinDirectory);

			var exeExtension = platformIdentifier.IsWindows
				? ".exe"
				: String.Empty;

			return new OpenDreamInstallation(
				IOManager.ConcatPath(
					binPathForVersion,
					$"OpenDreamServer{exeExtension}"),
				IOManager.ConcatPath(
					binPathForVersion,
					$"DMCompiler{exeExtension}"),
				installationTask,
				version);
		}

		/// <inheritdoc />
		public override async ValueTask<IEngineInstallationData> DownloadVersion(ByondVersion version, JobProgressReporter jobProgressReporter, CancellationToken cancellationToken)
		{
			CheckVersionValidity(version);

			// get a lock on a system wide OD repo
			Logger.LogTrace("Cloning OD repo...");

			var progressSection1 = jobProgressReporter?.CreateSection("Updating OpenDream git repository", 0.5f);

			var repo = await repositoryManager.CloneRepository(
				generalConfiguration.OpenDreamGitUrl,
				null,
				null,
				null,
				progressSection1,
				true,
				cancellationToken);

			try
			{
				if (repo == null)
				{
					Logger.LogTrace("OD repo seems to already exist, attempting load and fetch...");
					repo = await repositoryManager.LoadRepository(cancellationToken);

					await repo.FetchOrigin(
						progressSection1,
						null,
						null,
						false,
						cancellationToken);
				}

				var progressSection2 = jobProgressReporter?.CreateSection("Checking out OpenDream version", 0.5f);

				var committish = version.SourceSHA
					?? $"{generalConfiguration.OpenDreamGitTagPrefix}{version.Version.Semver()}";

				await repo.CheckoutObject(
					committish,
					null,
					null,
					true,
					progressSection2,
					cancellationToken);

				return new RepositoryEngineInstallationData(IOManager, repo, InstallationSourceSubDirectory);
			}
			catch
			{
				repo?.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public override async ValueTask Install(ByondVersion version, string installPath, CancellationToken cancellationToken)
		{
			CheckVersionValidity(version);
			ArgumentNullException.ThrowIfNull(installPath);
			var sourcePath = IOManager.ConcatPath(installPath, InstallationSourceSubDirectory);

			if (!await IOManager.DirectoryExists(sourcePath, cancellationToken))
			{
				// a zip install that didn't come from us?
				// we want to use the bin dir, so put everything where we expect
				Logger.LogDebug("Correcting extraction location...");
				var dirsTask = IOManager.GetDirectories(installPath, cancellationToken);
				var filesTask = IOManager.GetFiles(installPath, cancellationToken);
				var dirCreateTask = IOManager.CreateDirectory(sourcePath, cancellationToken);

				await Task.WhenAll(dirsTask, filesTask, dirCreateTask);

				var dirsMoveTasks = dirsTask
					.Result
					.Select(
						dirPath => IOManager.MoveDirectory(
							dirPath,
							IOManager.ConcatPath(
								sourcePath,
								IOManager.GetFileName(sourcePath)),
							cancellationToken));
				var filesMoveTask = filesTask
					.Result
					.Select(
						filePath => IOManager.MoveFile(
							filePath,
							IOManager.ConcatPath(
								sourcePath,
								IOManager.GetFileName(sourcePath)),
							cancellationToken));

				await Task.WhenAll(dirsMoveTasks.Concat(filesMoveTask));
			}

			var dotnetPaths = DotnetHelper.GetPotentialDotnetPaths(platformIdentifier.IsWindows)
				.ToList();
			var tasks = dotnetPaths
				.Select(path => IOManager.FileExists(path, cancellationToken))
				.ToList();

			await Task.WhenAll(tasks);

			var selectedPathIndex = tasks.FindIndex(pathValidTask => pathValidTask.Result);

			if (selectedPathIndex == -1)
				throw new JobException(ErrorCode.OpenDreamCantFindDotnet);

			var dotnetPath = dotnetPaths[selectedPathIndex];

			await using (var buildProcess = processExecutor.LaunchProcess(
				dotnetPath,
				sourcePath,
				"build -c Release /p:TgsEngineBuild=true",
				null,
				true,
				true))
			{
				var buildExitCode = await buildProcess.Lifetime;
				if (buildExitCode != 0)
					throw new JobException("OpenDream build failed!");
			}

			await IOManager.MoveDirectory(
				IOManager.ConcatPath(
					sourcePath,
					InstallationBinDirectory,
					"Content.Server"),
				IOManager.ConcatPath(
					installPath,
					InstallationBinDirectory),
				cancellationToken);

			await IOManager.DeleteDirectory(sourcePath, cancellationToken);
		}

		/// <inheritdoc />
		public override ValueTask UpgradeInstallation(ByondVersion version, string path, CancellationToken cancellationToken)
		{
			CheckVersionValidity(version);
			ArgumentNullException.ThrowIfNull(path);
			return ValueTask.CompletedTask;
		}

		/// <inheritdoc />
		public override ValueTask TrustDmbPath(string fullDmbPath, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(fullDmbPath);
			return ValueTask.CompletedTask;
		}
	}
}
