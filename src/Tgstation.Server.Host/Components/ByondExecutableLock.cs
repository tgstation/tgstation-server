using System;

namespace Tgstation.Server.Host.Components
{
	sealed class ByondExecutableLock : IByondExecutableLock
	{
		public Version Version { get; set; }

		public string DreamDaemonPath { get; set; }

		public string DreamMakerPath { get; set; }

		public void Dispose() { }

		public void DoNotDeleteThisSession() { }
	}
}
