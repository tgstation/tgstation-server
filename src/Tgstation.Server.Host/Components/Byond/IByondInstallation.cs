using System;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <summary>
	/// Represents a BYOND installation.
	/// </summary>
	public interface IByondInstallation
	{
		/// <summary>
		/// The <see cref="global::System.Version"/> of the <see cref="IByondInstallation"/>.
		/// </summary>
		Version Version { get; }

		/// <summary>
		/// The full path to the DreamDaemon executable.
		/// </summary>
		string DreamDaemonPath { get; }

		/// <summary>
		/// The full path to the dm/DreamMaker executable.
		/// </summary>
		string DreamMakerPath { get; }

		/// <summary>
		/// If <see cref="DreamDaemonPath"/> supports being run as a command-line application.
		/// </summary>
		bool SupportsCli { get; }

		/// <summary>
		/// If <see cref="DreamDaemonPath"/> supports the -map-threads parameter.
		/// </summary>
		bool SupportsMapThreads { get; }
	}
}
