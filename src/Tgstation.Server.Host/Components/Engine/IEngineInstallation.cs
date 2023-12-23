using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <summary>
	/// Represents a BYOND installation.
	/// </summary>
	public interface IEngineInstallation
	{
		/// <summary>
		/// The <see cref="EngineVersion"/> of the <see cref="IEngineInstallation"/>.
		/// </summary>
		EngineVersion Version { get; }

		/// <summary>
		/// The full path to the game server executable.
		/// </summary>
		string ServerExePath { get; }

		/// <summary>
		/// The full path to the dm/DreamMaker executable.
		/// </summary>
		string CompilerExePath { get; }

		/// <summary>
		/// If <see cref="ServerExePath"/> supports being run as a command-line application and outputs log information to be captured.
		/// </summary>
		bool HasStandardOutput { get; }

		/// <summary>
		/// If <see cref="ServerExePath"/> may create network prompts.
		/// </summary>
		bool PromptsForNetworkAccess { get; }

		/// <summary>
		/// If <see cref="HasStandardOutput"/> is set, this indicates that the engine server has good file logging that should be preferred to ours.
		/// </summary>
		bool PreferFileLogging { get; }

		/// <summary>
		/// The <see cref="Task"/> that completes when the BYOND version finished installing.
		/// </summary>
		Task InstallationTask { get; }

		/// <summary>
		/// Return the command line arguments for launching with given <paramref name="launchParameters"/>.
		/// </summary>
		/// <param name="dmbProvider">The <see cref="IDmbProvider"/>.</param>
		/// <param name="parameters">The map of parameter <see cref="string"/>s as a <see cref="IReadOnlyDictionary{TKey, TValue}"/>. MUST include <see cref="Interop.DMApiConstants.ParamAccessIdentifier"/>. Should NOT include the <see cref="DreamDaemonLaunchParameters.AdditionalParameters"/> of <paramref name="launchParameters"/>.</param>
		/// <param name="launchParameters">The <see cref="DreamDaemonLaunchParameters"/>.</param>
		/// <param name="logFilePath">The full path to the log file, if any.</param>
		/// <returns>The formatted arguments <see cref="string"/>.</returns>
		string FormatServerArguments(
			IDmbProvider dmbProvider,
			IReadOnlyDictionary<string, string> parameters,
			DreamDaemonLaunchParameters launchParameters,
			string? logFilePath);

		/// <summary>
		/// Return the command line arguments for compiling a given <paramref name="dmePath"/> if compilation is necessary.
		/// </summary>
		/// <param name="dmePath">The full path to the .dme to compile.</param>
		/// <returns>The formatted arguments <see cref="string"/>.</returns>
		string FormatCompilerArguments(string dmePath);

		/// <summary>
		/// Kills a given engine server <paramref name="process"/>.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="process">The <see cref="IProcess"/> to be terminated.</param>
		/// <param name="accessIdentifier">The <see cref="Interop.DMApiParameters.AccessIdentifier"/> of the session.</param>
		/// <param name="port">The port the server is running on.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask StopServerProcess(ILogger logger, IProcess process, string accessIdentifier, ushort port, CancellationToken cancellationToken);
	}
}
