using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <inheritdoc cref="IEngineExecutableLock" />
	class EngineExecutableLock : ReferenceCounter<IEngineInstallation>, IEngineExecutableLock
	{
		/// <inheritdoc />
		public EngineVersion Version => Instance.Version;

		/// <inheritdoc />
		public string ServerExePath => Instance.ServerExePath;

		/// <inheritdoc />
		public string CompilerExePath => Instance.CompilerExePath;

		/// <inheritdoc />
		public bool HasStandardOutput => Instance.HasStandardOutput;

		/// <inheritdoc />
		public bool PreferFileLogging => Instance.PreferFileLogging;

		/// <inheritdoc />
		public bool PromptsForNetworkAccess => Instance.PromptsForNetworkAccess;

		/// <inheritdoc />
		public Task InstallationTask => Instance.InstallationTask;

		/// <inheritdoc />
		public bool UseDotnetDump => Instance.UseDotnetDump;

		/// <inheritdoc />
		public void DoNotDeleteThisSession() => DangerousDropReference();

		/// <inheritdoc />
		public string FormatServerArguments(
			IDmbProvider dmbProvider,
			IReadOnlyDictionary<string, string>? parameters,
			DreamDaemonLaunchParameters launchParameters,
			string accessIdentifier,
			string? logFilePath)
			=> Instance.FormatServerArguments(
				dmbProvider,
				parameters,
				launchParameters,
				accessIdentifier,
				logFilePath);

		/// <inheritdoc />
		public string FormatCompilerArguments(string dmePath, string? additionalArguments) => Instance.FormatCompilerArguments(dmePath, additionalArguments);

		/// <inheritdoc />
		public ValueTask StopServerProcess(ILogger logger, IProcess process, string accessIdentifier, ushort port, CancellationToken cancellationToken)
			=> Instance.StopServerProcess(
				logger,
				process,
				accessIdentifier,
				port,
				cancellationToken);

		/// <inheritdoc />
		public ValueTask<Dictionary<string, string>?> LoadEnv(ILogger logger, bool forCompiler, CancellationToken cancellationToken)
			=> Instance.LoadEnv(logger, forCompiler, cancellationToken);
	}
}
