using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <summary>
	/// Implementation of <see cref="OpenDreamInstaller"/> for Windows systems.
	/// </summary>
	sealed class WindowsOpenDreamInstaller : OpenDreamInstaller
	{
		/// <summary>
		/// The <see cref="IFilesystemLinkFactory"/> for the <see cref="WindowsOpenDreamInstaller"/>.
		/// </summary>
		readonly IFilesystemLinkFactory linkFactory;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowsOpenDreamInstaller"/> class.
		/// </summary>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="OpenDreamInstaller"/>.</param>
		/// <param name="logger">The <see cref="ILogger{TCategoryName}"/> for the <see cref="OpenDreamInstaller"/>.</param>
		/// <param name="platformIdentifier">The <see cref="IPlatformIdentifier"/> for the <see cref="OpenDreamInstaller"/>.</param>
		/// <param name="processExecutor">The <see cref="IProcessExecutor"/> for the <see cref="OpenDreamInstaller"/>.</param>
		/// <param name="repositoryManager">The <see cref="IRepositoryManager"/> for the <see cref="OpenDreamInstaller"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> of <see cref="GeneralConfiguration"/> for the <see cref="OpenDreamInstaller"/>.</param>
		/// <param name="linkFactory">The value of <see cref="linkFactory"/>.</param>
		public WindowsOpenDreamInstaller(
			IIOManager ioManager,
			ILogger<WindowsOpenDreamInstaller> logger,
			IPlatformIdentifier platformIdentifier,
			IProcessExecutor processExecutor,
			IRepositoryManager repositoryManager,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IFilesystemLinkFactory linkFactory)
			: base(
				ioManager,
				logger,
				platformIdentifier,
				processExecutor,
				repositoryManager,
				generalConfigurationOptions)
		{
			this.linkFactory = linkFactory ?? throw new ArgumentNullException(nameof(linkFactory));
		}

		/// <inheritdoc />
		public override ValueTask Install(EngineVersion version, string installPath, CancellationToken cancellationToken)
			=> ValueTaskExtensions.WhenAll(
				base.Install(
					version,
					installPath,
					cancellationToken),
				AddServerFirewallException(
					version,
					installPath,
					cancellationToken));

		/// <inheritdoc />
		protected override async ValueTask HandleExtremelyLongPathOperation(Func<string, ValueTask> shortenedPathOperation, string originalPath, CancellationToken cancellationToken)
		{
			var shortPath = $"C:/{Guid.NewGuid()}";
			Logger.LogDebug("Shortening path for build from {long} to {short}...", originalPath, shortPath);
			await linkFactory.CreateSymbolicLink(originalPath, shortPath, cancellationToken);
			try
			{
				await shortenedPathOperation(shortPath);
			}
			finally
			{
				await IOManager.DeleteDirectory(shortPath, CancellationToken.None); // DCT: Should always run
			}
		}

		/// <summary>
		/// Attempt to add the DreamDaemon executable as an exception to the Windows firewall.
		/// </summary>
		/// <param name="version">The BYOND <see cref="EngineVersion"/>.</param>
		/// <param name="path">The path to the BYOND installation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask AddServerFirewallException(EngineVersion version, string path, CancellationToken cancellationToken)
		{
			if (GeneralConfiguration.SkipAddingByondFirewallException)
				return;

			GetExecutablePaths(path, out var serverExePath, out _);

			int exitCode;
			try
			{
				// I really wish we could add the instance name here but
				// 1. It'd make IByondInstaller need to be transient per-instance and WindowsByondInstaller relys on being a singleton for its DX installer call
				// 2. The instance could be renamed, so it'd have to be an unfriendly ID anyway.
				var ruleName = $"TGS DreamDaemon {version}";

				exitCode = await WindowsFirewallHelper.AddFirewallException(
					ProcessExecutor,
					Logger,
					ruleName,
					serverExePath,
					cancellationToken);
			}
			catch (Exception ex)
			{
				throw new JobException(ErrorCode.EngineFirewallFail, ex);
			}

			if (exitCode != 0)
				throw new JobException(ErrorCode.EngineFirewallFail, new JobException($"Invalid exit code: {exitCode}"));
		}
	}
}
