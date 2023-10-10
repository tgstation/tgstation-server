using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Common;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils.GitHub;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <summary>
	/// Implementation of <see cref="IEngineInstaller"/> for <see cref="EngineType.OpenDream"/>.
	/// </summary>
	sealed class OpenDreamInstaller : EngineInstallerBase
	{
		/// <inheritdoc />
		protected override EngineType TargetEngineType => EngineType.OpenDream;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="OpenDreamInstaller"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="IGitHubService"/> for the <see cref="OpenDreamInstaller"/>.
		/// </summary>
		readonly IGitHubService gitHubService;

		/// <summary>
		/// The <see cref="IProcessExecutor"/> for the <see cref="OpenDreamInstaller"/>.
		/// </summary>
		readonly IProcessExecutor processExecutor;

		/// <summary>
		/// Initializes a new instance of the <see cref="OpenDreamInstaller"/> class.
		/// </summary>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="EngineInstallerBase"/>.</param>
		/// <param name="fileDownloader">The <see cref="IFileDownloader"/> for the <see cref="EngineInstallerBase"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="EngineInstallerBase"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="gitHubService">The value of <see cref="gitHubService"/>.</param>
		/// <param name="processExecutor">The value of <see cref="processExecutor"/>.</param>
		public OpenDreamInstaller(
			IIOManager ioManager,
			IFileDownloader fileDownloader,
			ILogger<OpenDreamInstaller> logger,
			IPlatformIdentifier platformIdentifier,
			IGitHubService gitHubService,
			IProcessExecutor processExecutor)
			: base(ioManager, fileDownloader, logger)
		{
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.gitHubService = gitHubService ?? throw new ArgumentNullException(nameof(gitHubService));
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
		}

		/// <inheritdoc />
		public override Task CleanCache(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public override IEngineInstallation CreateInstallation(ByondVersion version, Task installationTask)
		{
			CheckVersionValidity(version);
			return new OpenDreamInstallation(installationTask, version);
		}

		/// <inheritdoc />
		public override async ValueTask Install(ByondVersion version, string path, CancellationToken cancellationToken)
		{
			CheckVersionValidity(version);
			ArgumentNullException.ThrowIfNull(path);

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

			await Task.Yield();
			throw new NotImplementedException();
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

		/// <inheritdoc />
		protected override async ValueTask<Uri> GetDownloadZipUrl(ByondVersion version, CancellationToken cancellationToken)
		{
			throw new NotImplementedException("This won't work because of the goddamn fucking robust toolbox submodule");
			var fullCommit = await gitHubService.GetCommit("OpenDreamProject", "OpenDream", version.SourceCommittish, cancellationToken);

			if (fullCommit.Sha != version.SourceCommittish)
			{
				Logger.LogInformation("Replacing committish {committish} with full SHA {sha}...", version.SourceCommittish, fullCommit.Sha);
				version.SourceCommittish = fullCommit.Sha;
			}

			var gitHubDownloadUrlString = $"https://codeload.github.com/OpenDreamProject/OpenDream/zip/{version.SourceCommittish}";
			return new Uri(gitHubDownloadUrlString);
		}
	}
}
