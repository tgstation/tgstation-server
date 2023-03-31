using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <summary>
	/// Represents usage of the two primary BYOND server executables.
	/// </summary>
	public interface IByondExecutableLock : IDisposable
	{
		/// <summary>
		/// The <see cref="global::System.Version"/> of the locked executables.
		/// </summary>
		Version Version { get; }

		/// <summary>
		/// The path to the DreamDaemon executable.
		/// </summary>
		string DreamDaemonPath { get; }

		/// <summary>
		/// The path to the dm/DreamMaker executable.
		/// </summary>
		string DreamMakerPath { get; }

		/// <summary>
		/// If <see cref="DreamDaemonPath"/> supports being run as a command-line application.
		/// </summary>
		bool SupportsCli { get; }

		/// <summary>
		/// Call if, during a detach, this version should not be deleted.
		/// </summary>
		void DoNotDeleteThisSession();

		/// <summary>
		/// Add a given <paramref name="fullDmbPath"/> to the trusted DMBs list in BYOND's config.
		/// </summary>
		/// <param name="fullDmbPath">Full path to the .dmb that should be trusted.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task TrustDmbPath(string fullDmbPath, CancellationToken cancellationToken);
	}
}
