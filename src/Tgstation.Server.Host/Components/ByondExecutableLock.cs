using System;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class ByondExecutableLock : IByondExecutableLock
	{
		/// <inheritdoc />
		public Version Version { get; set; }

		/// <inheritdoc />
		public string DreamDaemonPath { get; set; }

		/// <inheritdoc />
		public string DreamMakerPath { get; set; }

		//at one point in design, byond versions were to delete themselves if they werent the active version
		//That changed at some point so these functions are intentioanlly left blank

		/// <inheritdoc />
		public void Dispose() { }

		/// <inheritdoc />
		public void DoNotDeleteThisSession() { }
	}
}
