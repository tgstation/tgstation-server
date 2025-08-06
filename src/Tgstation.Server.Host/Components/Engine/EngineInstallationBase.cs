using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using DotEnv.Core;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <inheritdoc />
	abstract class EngineInstallationBase : IEngineInstallation
	{
		/// <inheritdoc />
		public abstract EngineVersion Version { get; }

		/// <inheritdoc />
		public abstract string ServerExePath { get; }

		/// <inheritdoc />
		public abstract string CompilerExePath { get; }

		/// <inheritdoc />
		public abstract bool HasStandardOutput { get; }

		/// <inheritdoc />
		public abstract bool PreferFileLogging { get; }

		/// <inheritdoc />
		public abstract bool PromptsForNetworkAccess { get; }

		/// <inheritdoc />
		public abstract bool UseDotnetDump { get; }

		/// <inheritdoc />
		public abstract Task InstallationTask { get; }

		/// <summary>
		/// The <see cref="IIOManager"/> pointing to the installation directory.
		/// </summary>
		protected IIOManager InstallationIOManager { get; }

		/// <summary>
		/// Encode given parameters for passing as world.params on the command line.
		/// </summary>
		/// <param name="parameters"><see cref="IReadOnlyDictionary{TKey, TValue}"/> of parameters to encode.</param>
		/// <param name="launchParameters">The active <see cref="DreamDaemonLaunchParameters"/>.</param>
		/// <returns>The formatted parameters <see cref="string"/>.</returns>
		protected static string EncodeParameters(
			IReadOnlyDictionary<string, string>? parameters,
			DreamDaemonLaunchParameters launchParameters)
		{
			var parametersString = parameters != null
				? $"{String.Join('&', parameters.Select(kvp => $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"))}&"
				: String.Empty;

			if (!String.IsNullOrEmpty(launchParameters.AdditionalParameters))
				parametersString += launchParameters.AdditionalParameters;

			return parametersString;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EngineInstallationBase"/> class.
		/// </summary>
		/// <param name="installationIOManager">The value of <see cref="InstallationIOManager"/>.</param>
		public EngineInstallationBase(IIOManager installationIOManager)
		{
			InstallationIOManager = installationIOManager ?? throw new ArgumentNullException(nameof(installationIOManager));
		}

		/// <inheritdoc />
		public abstract string FormatCompilerArguments(string dmePath, string? additionalArguments);

		/// <inheritdoc />
		public abstract string FormatServerArguments(
			IDmbProvider dmbProvider,
			IReadOnlyDictionary<string, string>? parameters,
			DreamDaemonLaunchParameters launchParameters,
			string accessIdentifier,
			string? logFilePath);

		/// <inheritdoc />
		public virtual async ValueTask StopServerProcess(ILogger logger, IProcess process, string accessIdentifier, ushort port, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(logger);
			cancellationToken.ThrowIfCancellationRequested();
			logger.LogTrace("Terminating engine server process...");
			process.Terminate();
			await process.Lifetime;
		}

		/// <inheritdoc />
		public async ValueTask<Dictionary<string, string>?> LoadEnv(ILogger logger, bool forCompiler, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(logger);

			var envFile = forCompiler
				? "compiler.env"
				: "server.env";

			if (!await InstallationIOManager.FileExists(envFile, cancellationToken))
			{
				logger.LogTrace("No {envFile} present in engine installation {version}", envFile, Version);
				return null;
			}

			logger.LogDebug("Loading {envFile} for engine installation {version}...", envFile, Version);

			var fileBytes = await InstallationIOManager.ReadAllBytes(envFile, cancellationToken);
			var fileContents = Encoding.UTF8.GetString(fileBytes.Span);
			var parser = new EnvParser();

			try
			{
				var variables = parser.Parse(fileContents);

				return variables.ToDictionary();
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Unable to parse {envFile}!", envFile);
				return null;
			}
		}
	}
}
