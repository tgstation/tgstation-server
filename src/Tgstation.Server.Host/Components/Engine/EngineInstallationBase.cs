using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.System;

#nullable disable

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
		public abstract Task InstallationTask { get; }

		/// <summary>
		/// Encode given parameters for passing as world.params on the command line.
		/// </summary>
		/// <param name="parameters"><see cref="IReadOnlyDictionary{TKey, TValue}"/> of parameters to encode.</param>
		/// <param name="launchParameters">The active <see cref="DreamDaemonLaunchParameters"/>.</param>
		/// <returns>The formatted parameters <see cref="string"/>.</returns>
		protected static string EncodeParameters(
			IReadOnlyDictionary<string, string> parameters,
			DreamDaemonLaunchParameters launchParameters)
		{
			var parametersString = String.Join('&', parameters.Select(kvp => $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));

			if (!String.IsNullOrEmpty(launchParameters.AdditionalParameters))
				parametersString = $"{parametersString}&{launchParameters.AdditionalParameters}";

			return parametersString;
		}

		/// <inheritdoc />
		public abstract string FormatCompilerArguments(string dmePath);

		/// <inheritdoc />
		public abstract string FormatServerArguments(
			IDmbProvider dmbProvider,
			IReadOnlyDictionary<string, string> parameters,
			DreamDaemonLaunchParameters launchParameters,
			string logFilePath);

		/// <inheritdoc />
		public virtual async ValueTask StopServerProcess(ILogger logger, IProcess process, string accessIdentifier, ushort port, CancellationToken cancellationToken)
		{
			logger.LogTrace("Terminating engine server process...");
			process.Terminate();
			await process.Lifetime;
		}
	}
}
