using System;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <inheritdoc />
	sealed class ByondInstallation : IByondInstallation
	{
		/// <inheritdoc />
		public Version Version { get; }

		/// <inheritdoc />
		public string DreamDaemonPath { get; }

		/// <inheritdoc />
		public string DreamMakerPath { get; }

		/// <inheritdoc />
		public bool SupportsCli { get; }

		/// <inheritdoc />
		public bool SupportsMapThreads => Version.Major >= 515 && Version.Minor >= 1605;

		/// <summary>
		/// The <see cref="Task"/> that completes when the BYOND version finished installing.
		/// </summary>
		public Task InstallationTask { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ByondInstallation"/> class.
		/// </summary>
		/// <param name="installationTask">The value of <see cref="InstallationTask"/>.</param>
		/// <param name="version">The value of <see cref="Version"/>.</param>
		/// <param name="dreamDaemonPath">The value of <see cref="DreamDaemonPath"/>.</param>
		/// <param name="dreamMakerPath">The value of <see cref="DreamMakerPath"/>.</param>
		/// <param name="supportsCli">The value of <see cref="SupportsCli"/>.</param>
		public ByondInstallation(
			Task installationTask,
			Version version,
			string dreamDaemonPath,
			string dreamMakerPath,
			bool supportsCli)
		{
			InstallationTask = installationTask ?? throw new ArgumentNullException(nameof(installationTask));
			Version = version ?? throw new ArgumentNullException(nameof(version));
			DreamDaemonPath = dreamDaemonPath ?? throw new ArgumentNullException(nameof(dreamDaemonPath));
			DreamMakerPath = dreamMakerPath ?? throw new ArgumentNullException(nameof(dreamMakerPath));
			SupportsCli = supportsCli;
		}
	}
}
