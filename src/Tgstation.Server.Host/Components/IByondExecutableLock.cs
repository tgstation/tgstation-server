using System;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Represents usage of the two primary BYOND server executables
	/// </summary>
	public interface IByondExecutableLock : IDisposable
	{
		/// <summary>
		/// The <see cref="System.Version"/> of the locked executables
		/// </summary>
		Version Version { get; }

		/// <summary>
		/// The path to the DreamDaemon executable
		/// </summary>
		string DreamDaemonPath { get; }

		/// <summary>
		/// The path to the dm/DreamMaker executable
		/// </summary>
		string DreamMakerPath { get; }

		/// <summary>
		/// Call if, during a detach, this version should not be deleted
		/// </summary>
		void DoNotDeleteThisSession();
	}
}
