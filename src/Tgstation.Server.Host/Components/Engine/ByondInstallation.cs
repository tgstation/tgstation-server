using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Engine
{
	/// <summary>
	/// Implementation of <see cref="IEngineInstallation"/> for <see cref="EngineType.Byond"/>.
	/// </summary>
	sealed class ByondInstallation : EngineInstallationBase
	{
		/// <inheritdoc />
		public override EngineVersion Version { get; }

		/// <inheritdoc />
		public override string ServerExePath { get; }

		/// <inheritdoc />
		public override string CompilerExePath { get; }

		/// <inheritdoc />
		public override bool PromptsForNetworkAccess { get; }

		/// <inheritdoc />
		public override bool HasStandardOutput { get; }

		/// <inheritdoc />
		public override bool PreferFileLogging => false;

		/// <inheritdoc />
		public override bool UseDotnetDump => false;

		/// <inheritdoc />
		public override Task InstallationTask { get; }

		/// <summary>
		/// If map threads are supported by the <see cref="Version"/>.
		/// </summary>
		readonly bool supportsMapThreads;

		/// <summary>
		/// Change a given <paramref name="securityLevel"/> into the appropriate DreamDaemon command line word.
		/// </summary>
		/// <param name="securityLevel">The <see cref="DreamDaemonSecurity"/> level to change.</param>
		/// <returns>A <see cref="string"/> representation of the command line parameter.</returns>
		static string SecurityWord(DreamDaemonSecurity securityLevel)
		{
			return securityLevel switch
			{
				DreamDaemonSecurity.Safe => "safe",
				DreamDaemonSecurity.Trusted => "trusted",
				DreamDaemonSecurity.Ultrasafe => "ultrasafe",
				_ => throw new ArgumentOutOfRangeException(nameof(securityLevel), securityLevel, String.Format(CultureInfo.InvariantCulture, "Bad DreamDaemon security level: {0}", securityLevel)),
			};
		}

		/// <summary>
		/// Change a given <paramref name="visibility"/> into the appropriate DreamDaemon command line word.
		/// </summary>
		/// <param name="visibility">The <see cref="DreamDaemonVisibility"/> level to change.</param>
		/// <returns>A <see cref="string"/> representation of the command line parameter.</returns>
		static string VisibilityWord(DreamDaemonVisibility visibility)
		{
			return visibility switch
			{
				DreamDaemonVisibility.Public => "public",
				DreamDaemonVisibility.Private => "private",
				DreamDaemonVisibility.Invisible => "invisible",
				_ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, String.Format(CultureInfo.InvariantCulture, "Bad DreamDaemon visibility level: {0}", visibility)),
			};
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ByondInstallation"/> class.
		/// </summary>
		/// <param name="installationIOManager">The <see cref="IIOManager"/> for the <see cref="EngineInstallationBase"/>.</param>
		/// <param name="installationTask">The value of <see cref="InstallationTask"/>.</param>
		/// <param name="version">The value of <see cref="Version"/>.</param>
		/// <param name="dreamDaemonPath">The value of <see cref="ServerExePath"/>.</param>
		/// <param name="dreamMakerPath">The value of <see cref="CompilerExePath"/>.</param>
		/// <param name="supportsCli">If a CLI application is being used.</param>
		/// <param name="supportsMapThreads">The value of <see cref="supportsMapThreads"/>.</param>
		public ByondInstallation(
			IIOManager installationIOManager,
			Task installationTask,
			EngineVersion version,
			string dreamDaemonPath,
			string dreamMakerPath,
			bool supportsCli,
			bool supportsMapThreads)
			: base(installationIOManager)
		{
			InstallationTask = installationTask ?? throw new ArgumentNullException(nameof(installationTask));
			ArgumentNullException.ThrowIfNull(version);

			if (version.Engine != EngineType.Byond)
				throw new ArgumentException($"Invalid EngineType: {version.Engine}", nameof(version));

			Version = version ?? throw new ArgumentNullException(nameof(version));
			ServerExePath = dreamDaemonPath ?? throw new ArgumentNullException(nameof(dreamDaemonPath));
			CompilerExePath = dreamMakerPath ?? throw new ArgumentNullException(nameof(dreamMakerPath));
			HasStandardOutput = supportsCli;
			PromptsForNetworkAccess = !supportsCli;
			this.supportsMapThreads = supportsMapThreads;
		}

		/// <inheritdoc />
		public override string FormatServerArguments(
			IDmbProvider dmbProvider,
			IReadOnlyDictionary<string, string>? parameters,
			DreamDaemonLaunchParameters launchParameters,
			string accessIdentifier,
			string? logFilePath)
		{
			ArgumentNullException.ThrowIfNull(dmbProvider);
			ArgumentNullException.ThrowIfNull(launchParameters);
			ArgumentNullException.ThrowIfNull(accessIdentifier);

			var encodedParameters = EncodeParameters(parameters, launchParameters);
			var parametersString = !String.IsNullOrEmpty(encodedParameters)
				? $" -params \"{encodedParameters}\""
				: String.Empty;

			// important to run on all ports to allow port changing
			var arguments = String.Format(
				CultureInfo.InvariantCulture,
				"\"{0}\" -port {1} -ports 1-65535 {2}-close -verbose -{3} -{4}{5}{6}{7}{8}",
				dmbProvider.DmbName,
				launchParameters.Port!.Value,
				launchParameters.AllowWebClient!.Value
					? "-webclient "
					: String.Empty,
				SecurityWord(launchParameters.SecurityLevel!.Value),
				VisibilityWord(launchParameters.Visibility!.Value),
				logFilePath != null
					? $" -logself -log {logFilePath}"
					: String.Empty, // DD doesn't output anything if -logself is set???
				launchParameters.StartProfiler!.Value
					? " -profile"
					: String.Empty,
				supportsMapThreads && launchParameters.MapThreads!.Value != 0
					? $" -map-threads {launchParameters.MapThreads.Value}"
					: String.Empty,
				parametersString);
			return arguments;
		}

		/// <inheritdoc />
		public override string FormatCompilerArguments(string dmePath, string? additionalArguments)
		{
			if (String.IsNullOrWhiteSpace(additionalArguments))
				additionalArguments = String.Empty;
			else
				additionalArguments = $"{additionalArguments.Trim()} ";

			return $"-clean {additionalArguments}\"{dmePath ?? throw new ArgumentNullException(nameof(dmePath))}\"";
		}
	}
}
