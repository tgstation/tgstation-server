using System.Collections.Generic;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <inheritdoc cref="IEngineExecutableLock" />
	sealed class EngineExecutableLock : ReferenceCounter<IEngineInstallation>, IEngineExecutableLock
	{
		/// <inheritdoc />
		public ByondVersion Version => Instance.Version;

		/// <inheritdoc />
		public string ServerExePath => Instance.ServerExePath;

		/// <inheritdoc />
		public string CompilerExePath => Instance.CompilerExePath;

		/// <inheritdoc />
		public bool HasStandardOutput => Instance.HasStandardOutput;

		/// <inheritdoc />
		public bool PromptsForNetworkAccess => Instance.PromptsForNetworkAccess;

		/// <inheritdoc />
		public Task InstallationTask => Instance.InstallationTask;

		/// <inheritdoc />
		public void DoNotDeleteThisSession() => DangerousDropReference();

		/// <inheritdoc />
		public string FormatServerArguments(
			IDmbProvider dmbProvider,
			IReadOnlyDictionary<string, string> parameters,
			DreamDaemonLaunchParameters launchParameters,
			string logFilePath)
			=> Instance.FormatServerArguments(
				dmbProvider,
				parameters,
				launchParameters,
				logFilePath);

		/// <inheritdoc />
		public string FormatCompilerArguments(string dmePath) => Instance.FormatCompilerArguments(dmePath);
	}
}
