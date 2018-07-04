using System;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Represents usage of the two primary BYOND server executables
	/// </summary>
	public interface IByondExecutableLock : IDisposable
	{
		/// <summary>
		/// The path to the DreamDaemon executable
		/// </summary>
		string DreamDaemonPath { get; set; }

		/// <summary>
		/// The path to the dm/DreamMaker executable
		/// </summary>
		string DreamMakerPath { get; set; }

		/// <summary>
		/// Call if, during a detach, this version should not be deleted
		/// </summary>
		void DoNotDeleteThisSession();
	}
}
