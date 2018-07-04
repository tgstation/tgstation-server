using System;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Represents a dream daemon process
	/// </summary>
	interface ISession : IDisposable
	{
		/// <summary>
		/// The <see cref="System.Diagnostics.Process.Id"/>
		/// </summary>
		int ProcessId { get; }

		/// <summary>
		/// A <see cref="Task"/> that completes when DreamDaemon starts pumping the windows message queue after loading a .dmb
		/// </summary>
		Task SuccessfulStartup { get; }

		/// <summary>
		/// A <see cref="Task"/> representing the lifetime of the <see cref="System.Diagnostics.Process"/> and resulting in the <see cref="System.Diagnostics.Process.ExitCode"/>
		/// </summary>
		Task<int> Lifetime { get; }

		/// <summary>
		/// Terminates the running process
		/// </summary>
		void Terminate();
	}
}