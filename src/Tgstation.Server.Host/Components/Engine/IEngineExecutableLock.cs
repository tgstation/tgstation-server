using System;

#nullable disable

namespace Tgstation.Server.Host.Components.Engine
{
	/// <summary>
	/// Represents usage of the two primary BYOND server executables.
	/// </summary>
	public interface IEngineExecutableLock : IEngineInstallation, IDisposable
	{
		/// <summary>
		/// Call if, during a detach, this version should not be deleted.
		/// </summary>
		void DoNotDeleteThisSession();
	}
}
