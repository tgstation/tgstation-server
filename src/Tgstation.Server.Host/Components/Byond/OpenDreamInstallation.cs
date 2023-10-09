using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Deployment;

namespace Tgstation.Server.Host.Components.Byond
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
		/// <param name="installationTask">The value of <see cref="InstallationTask"/>.</param>
		/// <param name="version">The value of <see cref="Version"/>.</param>
		public OpenDreamInstallation(
			Task installationTask,
			ByondVersion version)
		{
			InstallationTask = installationTask ?? throw new ArgumentNullException(nameof(installationTask));
			ArgumentNullException.ThrowIfNull(version);

			if (version.Engine.Value != EngineType.OpenDream)
				throw new ArgumentException($"Invalid EngineType: {version.Engine.Value}", nameof(version));

			Version = version ?? throw new ArgumentNullException(nameof(version));

			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string FormatServerArguments(IDmbProvider dmbProvider, IReadOnlyDictionary<string, string> parameters, DreamDaemonLaunchParameters launchParameters, string logFilePath)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public string FormatCompilerArguments(string dmePath)
		{
			ArgumentNullException.ThrowIfNull(dmePath);
			return null;
		}
	}
}
