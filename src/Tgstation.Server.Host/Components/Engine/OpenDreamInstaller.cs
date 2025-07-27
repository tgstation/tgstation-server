using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <summary>
	/// Implementation of <see cref="IEngineInstaller"/> for <see cref="EngineType.OpenDream"/>.
	/// </summary>
	class OpenDreamInstaller : EngineInstallerBase
	{
		/// <summary>
		/// The bin directory name.
		/// </summary>
		const string BinDir = "bin";

		/// <summary>
		/// The OD server directory name.
		/// </summary>
		const string ServerDir = "server";

		/// <summary>
		/// The name of the subdirectory in an installation's <see cref="BinDir"/> used to store the compiler binaries.
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
		/// The <see cref="GeneralConfiguration"/> for the <see cref="OpenDreamInstaller"/>.
		/// </summary>
		protected GeneralConfiguration GeneralConfiguration { get; }

		/// <summary>
		/// The <see cref="Configuration.SessionConfiguration"/> for the <see cref="OpenDreamInstaller"/>.
		/// </summary>
		protected SessionConfiguration SessionConfiguration { get; }

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="OpenDreamInstaller"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="IRepositoryManager"/> for the OpenDream repository.
		/// </summary>
		readonly IRepositoryManager repositoryManager;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="OpenDreamInstaller"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="IHttpClientFactory"/> for the <see cref="OpenDreamInstaller"/>.
		/// </summary>
		readonly IHttpClientFactory httpClientFactory;

		/// <summary>
		/// Initializes a new instance of the <see cref="OpenDreamInstaller"/> class.
		/// </summary>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="EngineInstallerBase"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="EngineInstallerBase"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="processExecutor">The value of <see cref="ProcessExecutor"/>.</param>
		/// <param name="repositoryManager">The value of <see cref="repositoryManager"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="httpClientFactory">The value of <see cref="httpClientFactory"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="GeneralConfiguration"/>.</param>
		/// <param name="sessionConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="SessionConfiguration"/>.</param>
		public OpenDreamInstaller(
			IIOManager ioManager,
			ILogger<OpenDreamInstaller> logger,
			IPlatformIdentifier platformIdentifier,
			IProcessExecutor processExecutor,
			IRepositoryManager repositoryManager,
			IAsyncDelayer asyncDelayer,
			IHttpClientFactory httpClientFactory,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<SessionConfiguration> sessionConfigurationOptions)
			: base(ioManager, logger)
		{
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			ProcessExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			this.repositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
			GeneralConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			SessionConfiguration = sessionConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(sessionConfigurationOptions));
		}

		/// <inheritdoc />
		public override Task CleanCache(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public override async ValueTask<IEngineInstallation> CreateInstallation(EngineVersion version, string path, Task installationTask, CancellationToken cancellationToken)
		{
			CheckVersionValidity(version);
			GetExecutablePaths(path, out var serverExePath, out var compilerExePath);

			var dotnetPath = (await DotnetHelper.GetDotnetPath(platformIdentifier, IOManager, cancellationToken))
				?? throw new JobException("Failed to find dotnet path!");
			return new OpenDreamInstallation(
				IOManager.CreateResolverForSubdirectory(path),
				asyncDelayer,
				httpClientFactory,
				dotnetPath,
				serverExePath,
				compilerExePath,
				installationTask,
				version);
		}

		/// <inheritdoc />
		public override async ValueTask<IEngineInstallationData> DownloadVersion(EngineVersion version, JobProgressReporter jobProgressReporter, CancellationToken cancellationToken)
		{
			CheckVersionValidity(version);
			ArgumentNullException.ThrowIfNull(jobProgressReporter);

			// get a lock on a system wide OD repo
			Logger.LogTrace("Cloning OD repo...");

			var progressSection1 = jobProgressReporter.CreateSection("Updating OpenDream git repository", 0.5f);
			IRepository? repo;
			try
			{
				repo = await repositoryManager.CloneRepository(
					GeneralConfiguration.OpenDreamGitUrl,
					null,
					null,
					null,
					progressSection1,
					true,
					cancellationToken);
			}
			catch
			{
				progressSection1.Dispose();
				throw;
			}

			try
			{
				if (repo == null)
				{
					Logger.LogTrace("OD repo seems to already exist, attempting load and fetch...");
					repo = await repositoryManager.LoadRepository(cancellationToken);
					if (repo == null)
						throw new JobException("Can't load OpenDream repository! Please delete cache from disk!");

					await repo!.FetchOrigin(
						progressSection1,
						null,
						null,
						false,
						cancellationToken);
				}

				progressSection1.Dispose();
				progressSection1 = null;

				using (var progressSection2 = jobProgressReporter.CreateSection("Checking out OpenDream version", 0.5f))
				{
					var committish = version.SourceSHA
						?? $"{GeneralConfiguration.OpenDreamGitTagPrefix}{version.Version!.Semver()}";

					await repo.CheckoutObject(
						committish,
						null,
						null,
						true,
						false,
						progressSection2,
						cancellationToken);
				}

				if (!await repo.CommittishIsParent("tgs-min-compat", cancellationToken))
					throw new JobException(ErrorCode.OpenDreamTooOld);

				return new RepositoryEngineInstallationData(IOManager, repo, InstallationSourceSubDirectory);
			}
			catch
			{
				repo?.Dispose();
				throw;
			}
			finally
			{
				progressSection1?.Dispose();
			}
		}

		/// <inheritdoc />
		public override async ValueTask Install(EngineVersion version, string installPath, bool deploymentPipelineProcesses, CancellationToken cancellationToken)
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
								IOManager.GetFileName(dirPath)),
							cancellationToken));
				var filesMoveTask = filesTask
					.Result
					.Select(
						filePath => IOManager.MoveFile(
							filePath,
							IOManager.ConcatPath(
								sourcePath,
								IOManager.GetFileName(filePath)),
							cancellationToken));

				await Task.WhenAll(dirsMoveTasks.Concat(filesMoveTask));
			}

			var dotnetPath = (await DotnetHelper.GetDotnetPath(platformIdentifier, IOManager, cancellationToken))
				?? throw new JobException(ErrorCode.OpenDreamCantFindDotnet);
			const string DeployDir = "tgs_deploy";
			int? buildExitCode = null;
			await HandleExtremelyLongPathOperation(
				async shortenedPath =>
				{
					var shortenedDeployPath = IOManager.ConcatPath(shortenedPath, DeployDir);
					await using var buildProcess = await ProcessExecutor.LaunchProcess(
						dotnetPath,
						shortenedPath,
						$"run -c Release --project OpenDreamPackageTool -- --tgs -o {shortenedDeployPath}",
						cancellationToken,
						null,
						null,
						!GeneralConfiguration.OpenDreamSuppressInstallOutput,
						!GeneralConfiguration.OpenDreamSuppressInstallOutput);

					if (deploymentPipelineProcesses && SessionConfiguration.LowPriorityDeploymentProcesses)
						buildProcess.AdjustPriority(false);

					using (cancellationToken.Register(() => buildProcess.Terminate()))
						buildExitCode = await buildProcess.Lifetime;

					string? output;
					if (!GeneralConfiguration.OpenDreamSuppressInstallOutput)
					{
						var buildOutputTask = buildProcess.GetCombinedOutput(cancellationToken);
						if (!buildOutputTask.IsCompleted)
							Logger.LogTrace("OD build complete, waiting for output...");
						output = await buildOutputTask;
					}
					else
						output = "<Build output suppressed by configuration due to not being immediately available>";

					Logger.LogDebug(
						"OpenDream build exited with code {exitCode}:{newLine}{output}",
						buildExitCode,
						Environment.NewLine,
						output);
				},
				sourcePath,
				cancellationToken);

			if (buildExitCode != 0)
				throw new JobException("OpenDream build failed!");

			var deployPath = IOManager.ConcatPath(sourcePath, DeployDir);
			async ValueTask MoveDirs()
			{
				var dirs = await IOManager.GetDirectories(deployPath, cancellationToken);
				await Task.WhenAll(
					dirs.Select(
						dir => IOManager.MoveDirectory(
							dir,
							IOManager.ConcatPath(
								installPath,
								IOManager.GetFileName(dir)),
							cancellationToken)));
			}

			async ValueTask MoveFiles()
			{
				var files = await IOManager.GetFiles(deployPath, cancellationToken);
				await Task.WhenAll(
					files.Select(
						file => IOManager.MoveFile(
							file,
							IOManager.ConcatPath(
								installPath,
								IOManager.GetFileName(file)),
							cancellationToken)));
			}

			var dirsMoveTask = MoveDirs();
			var outputFilesMoveTask = MoveFiles();
			await ValueTaskExtensions.WhenAll(dirsMoveTask, outputFilesMoveTask);
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
		public override ValueTask TrustDmbPath(EngineVersion engineVersion, string fullDmbPath, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(engineVersion);
			ArgumentNullException.ThrowIfNull(fullDmbPath);

			Logger.LogTrace("TrustDmbPath is a no-op: {path}", fullDmbPath);
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
			const string DllExtension = ".dll";

			serverExePath = IOManager.ConcatPath(
				installationPath,
				BinDir,
				ServerDir,
				$"Robust.Server{DllExtension}");

			compilerExePath = IOManager.ConcatPath(
				installationPath,
				BinDir,
				InstallationCompilerDirectory,
				$"DMCompiler{DllExtension}");
		}
	}
}
