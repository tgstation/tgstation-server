using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <inheritdoc />
	abstract class EngineInstallerBase : IEngineInstaller
	{
		/// <summary>
		/// The <see cref="EngineType"/> the installer supports.
		/// </summary>
		protected abstract EngineType TargetEngineType { get; }

		/// <summary>
		/// Gets the <see cref="IIOManager"/> for the <see cref="EngineInstallerBase"/>.
		/// </summary>
		protected IIOManager IOManager { get; }

		/// <summary>
		/// Gets the <see cref="ILogger"/> for the <see cref="EngineInstallerBase"/>.
		/// </summary>
		protected ILogger<EngineInstallerBase> Logger { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="EngineInstallerBase"/> class.
		/// </summary>
		/// <param name="ioManager">The value of <see cref="IOManager"/>.</param>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		protected EngineInstallerBase(IIOManager ioManager, ILogger<EngineInstallerBase> logger)
		{
			IOManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public abstract ValueTask<IEngineInstallation> CreateInstallation(EngineVersion version, string path, Task installationTask, CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract Task CleanCache(CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract ValueTask Install(EngineVersion version, string path, bool deploymentPipelineProcesses, CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract ValueTask UpgradeInstallation(EngineVersion version, string path, CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract ValueTask<IEngineInstallationData> DownloadVersion(EngineVersion version, JobProgressReporter jobProgressReporter, CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract ValueTask TrustDmbPath(EngineVersion version, string fullDmbPath, CancellationToken cancellationToken);

		/// <summary>
		/// Check that a given <paramref name="version"/> is of type <see cref="EngineType.Byond"/>.
		/// </summary>
		/// <param name="version">The <see cref="EngineVersion"/> to check.</param>
		protected void CheckVersionValidity(EngineVersion version)
		{
			ArgumentNullException.ThrowIfNull(version);
			if (version.Engine!.Value != TargetEngineType)
				throw new InvalidOperationException($"Non-{TargetEngineType} engine specified: {version.Engine.Value}");
		}
	}
}
