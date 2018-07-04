using System;

namespace Tgstation.Server.Host.Components
{
	public interface IByondExecutableLock : IDisposable
	{
		string DreamDaemonPath { get; set; }
		string DreamMakerPath { get; set; }

		void DoNotDeleteThisSession();
	}
}
