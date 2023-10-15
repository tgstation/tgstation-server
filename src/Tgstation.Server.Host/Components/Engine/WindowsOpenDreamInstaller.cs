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
		/// Initializes a new instance of the <see cref="WindowsOpenDreamInstaller"/> class.
		/// </summary>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="OpenDreamInstaller"/>.</param>
		/// <param name="logger">The <see cref="ILogger{TCategoryName}"/> for the <see cref="OpenDreamInstaller"/>.</param>
		/// <param name="platformIdentifier">The <see cref="IPlatformIdentifier"/> for the <see cref="OpenDreamInstaller"/>.</param>
		/// <param name="processExecutor">The <see cref="IProcessExecutor"/> for the <see cref="OpenDreamInstaller"/>.</param>
		/// <param name="repositoryManager">The <see cref="IRepositoryManager"/> for the <see cref="OpenDreamInstaller"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> of <see cref="GeneralConfiguration"/> for the <see cref="OpenDreamInstaller"/>.</param>
		public WindowsOpenDreamInstaller(
			IIOManager ioManager,
			ILogger<WindowsOpenDreamInstaller> logger,
			IPlatformIdentifier platformIdentifier,
			IProcessExecutor processExecutor,
			IRepositoryManager repositoryManager,
			IOptions<GeneralConfiguration> generalConfigurationOptions)
			: base(
				ioManager,
				logger,
				platformIdentifier,
				processExecutor,
				repositoryManager,
				generalConfigurationOptions)
		{
		}

		/// <inheritdoc />
		public override ValueTask Install(ByondVersion version, string installPath, CancellationToken cancellationToken)
			=> ValueTaskExtensions.WhenAll(
				base.Install(
					version,
					installPath,
					cancellationToken),
				AddServerFirewallException(
					version,
					installPath,
					cancellationToken));

		/// <summary>
		/// Attempt to add the DreamDaemon executable as an exception to the Windows firewall.
		/// </summary>
		/// <param name="version">The BYOND <see cref="ByondVersion"/>.</param>
		/// <param name="path">The path to the BYOND installation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask AddServerFirewallException(ByondVersion version, string path, CancellationToken cancellationToken)
		{
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
