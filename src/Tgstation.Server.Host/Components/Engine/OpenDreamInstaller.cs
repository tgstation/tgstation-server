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
	class OpenDreamInstaller : EngineInstallerBase
	{
		/// <summary>
		/// The name of the subdirectory used to store the server binaries.
		/// </summary>
		const string InstallationServerDirectory = "server";

		/// <summary>
		/// The name of the subdirectory used to store the compiler binaries.
		/// </summary>
		const string InstallationCompilerDirectory = "compiler";

		/// <summary>
		/// The name of the subdirectory used for the <see cref="RepositoryEngineInstallationData"/>'s copy.
		/// </summary>
		const string InstallationSourceSubDirectory = "TgsSourceSubdir";

		/// <inheritdoc />
		protected override EngineType TargetEngineType => EngineType.OpenDream;

		/// <summary>
		/// The <see cref="IProcessExecutor"/> for the <see cref="OpenDreamInstaller"/>.
		/// </summary>
		protected IProcessExecutor ProcessExecutor { get; }

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="OpenDreamInstaller"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

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
		/// <param name="processExecutor">The value of <see cref="ProcessExecutor"/>.</param>
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
			ProcessExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			this.repositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <inheritdoc />
		public override Task CleanCache(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public override IEngineInstallation CreateInstallation(EngineVersion version, string path, Task installationTask)
		{
			CheckVersionValidity(version);
			GetExecutablePaths(path, out var serverExePath, out var compilerExePath);
			return new OpenDreamInstallation(
				serverExePath,
				compilerExePath,
				installationTask,
				version);
		}

		/// <inheritdoc />
		public override async ValueTask<IEngineInstallationData> DownloadVersion(EngineVersion version, JobProgressReporter jobProgressReporter, CancellationToken cancellationToken)
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
		public override async ValueTask Install(EngineVersion version, string installPath, CancellationToken cancellationToken)
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

			int? buildExitCode = null;
			await HandleExtremelyLongPathOperation(
				async shortenedPath =>
				{
					await using var buildProcess = ProcessExecutor.LaunchProcess(
						dotnetPath,
						shortenedPath,
						"build -c Release /p:TgsEngineBuild=true",
						null,
						true,
						true);
					buildExitCode = await buildProcess.Lifetime;
					Logger.LogDebug("Build output:{newLine}{output}", Environment.NewLine, await buildProcess.GetCombinedOutput(cancellationToken));
				},
				sourcePath,
				cancellationToken);

			if (buildExitCode != 0)
				throw new JobException("OpenDream build failed!");

			var serverMoveTask = IOManager.MoveDirectory(
				IOManager.ConcatPath(
					sourcePath,
					"bin",
					"Content.Server"),
				IOManager.ConcatPath(
					installPath,
					InstallationServerDirectory),
				cancellationToken);

			var compilerMoveTask = IOManager.MoveDirectory(
				IOManager.ConcatPath(
					sourcePath,
					"bin",
					"DMCompiler"),
				IOManager.ConcatPath(
					installPath,
					InstallationCompilerDirectory),
				cancellationToken);

			await Task.WhenAll(serverMoveTask, compilerMoveTask);
			await IOManager.DeleteDirectory(sourcePath, cancellationToken);
		}

		/// <inheritdoc />
		public override ValueTask UpgradeInstallation(EngineVersion version, string path, CancellationToken cancellationToken)
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

		/// <summary>
		/// Perform an operation on a very long path.
		/// </summary>
		/// <param name="shortenedPathOperation">A <see cref="Func{T, TResult}"/> taking a shortened path and resulting in a <see cref="ValueTask"/> representing the running operation.</param>
		/// <param name="originalPath">The original path to the directory.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected virtual ValueTask HandleExtremelyLongPathOperation(
			Func<string, ValueTask> shortenedPathOperation,
			string originalPath,
			CancellationToken cancellationToken)
			=> shortenedPathOperation(originalPath); // based god linux has no such weakness

		/// <summary>
		/// Gets the paths to the server and client executables.
		/// </summary>
		/// <param name="installationPath">The path to the OpenDream installation.</param>
		/// <param name="serverExePath">The path to the OpenDreamServer executable.</param>
		/// <param name="compilerExePath">The path to the DMCompiler executable.</param>
		protected void GetExecutablePaths(string installationPath, out string serverExePath, out string compilerExePath)
		{
			var exeExtension = platformIdentifier.IsWindows
				? ".exe"
				: String.Empty;

			serverExePath = IOManager.ConcatPath(
				installationPath,
				InstallationServerDirectory,
				$"OpenDreamServer{exeExtension}");

			compilerExePath = IOManager.ConcatPath(
				installationPath,
				InstallationCompilerDirectory,
				$"DMCompiler{exeExtension}");
		}
	}
}
