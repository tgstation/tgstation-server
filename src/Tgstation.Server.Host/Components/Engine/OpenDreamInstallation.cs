using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Deployment;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <summary>
	/// Implementation of <see cref="IEngineInstallation"/> for <see cref="EngineType.OpenDream"/>.
	/// </summary>
	sealed class OpenDreamInstallation : IEngineInstallation
	{
		/// <inheritdoc />
		public ByondVersion Version { get; }

		/// <inheritdoc />
		public string ServerExePath { get; }

		/// <inheritdoc />
		public string CompilerExePath { get; }

		/// <inheritdoc />
		public bool PromptsForNetworkAccess => false;

		/// <inheritdoc />
		public bool HasStandardOutput => true;

		/// <inheritdoc />
		public Task InstallationTask { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OpenDreamInstallation"/> class.
		/// </summary>
		/// <param name="serverExePath">The value of <see cref="ServerExePath"/>.</param>
		/// <param name="compilerExePath">The value of <see cref="CompilerExePath"/>.</param>
		/// <param name="installationTask">The value of <see cref="InstallationTask"/>.</param>
		/// <param name="version">The value of <see cref="Version"/>.</param>
		public OpenDreamInstallation(
			string serverExePath,
			string compilerExePath,
			Task installationTask,
			ByondVersion version)
		{
			ServerExePath = serverExePath ?? throw new ArgumentNullException(nameof(serverExePath));
			CompilerExePath = compilerExePath ?? throw new ArgumentNullException(nameof(compilerExePath));
			InstallationTask = installationTask ?? throw new ArgumentNullException(nameof(installationTask));
			Version = version ?? throw new ArgumentNullException(nameof(version));

			if (version.Engine.Value != EngineType.OpenDream)
				throw new ArgumentException($"Invalid EngineType: {version.Engine.Value}", nameof(version));
		}

		/// <inheritdoc />
		public string FormatServerArguments(IDmbProvider dmbProvider, IReadOnlyDictionary<string, string> parameters, DreamDaemonLaunchParameters launchParameters, string logFilePath)
		{
			ArgumentNullException.ThrowIfNull(dmbProvider);
			ArgumentNullException.ThrowIfNull(parameters);
			ArgumentNullException.ThrowIfNull(launchParameters);

			var parametersString = String.Join(';', parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));

			if (!String.IsNullOrEmpty(launchParameters.AdditionalParameters))
			{
				// TGS and BYOND expect url encoded params, OD takes unencoded
				var unencodedAdditionalParams = String.Join(
					';',
					launchParameters
						.AdditionalParameters
						.Split('&')
						.Select(
							singleParam => String.Join(
								'=',
								singleParam
									.Split('=')
									.Select(
										encodedParam => HttpUtility.UrlDecode(encodedParam)))));

				parametersString = $"{parametersString};{unencodedAdditionalParams}";
			}

			var arguments = $"--cvar net.port={launchParameters.Port.Value} --cvar opendream.topic_port=0 --cvar opendream.world_params=\"{parametersString}\" \"{dmbProvider.DmbName}\"";
			return arguments;
		}

		/// <inheritdoc />
		public string FormatCompilerArguments(string dmePath)
			=> $"--suppress-unimplemented --notices-enabled \"{dmePath ?? throw new ArgumentNullException(nameof(dmePath))}\"";
	}
}
