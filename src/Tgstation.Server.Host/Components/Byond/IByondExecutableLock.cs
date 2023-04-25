using System;

namespace Tgstation.Server.Host.Components.Byond
{
	/// <summary>
	/// Represents usage of the two primary BYOND server executables.
	/// </summary>
	public interface IByondExecutableLock : IByondInstallation, IDisposable
	{
		/// <summary>
		/// Call if, during a detach, this version should not be deleted.
		/// </summary>
		void DoNotDeleteThisSession();
	}
}
