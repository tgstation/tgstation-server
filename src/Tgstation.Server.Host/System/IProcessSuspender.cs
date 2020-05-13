namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// Abstraction for suspending and resuming processes.
	/// </summary>
	interface IProcessSuspender
	{
		/// <summary>
		/// Suspend a given <see cref="Process"/>.
		/// </summary>
		/// <param name="process">The <see cref="Process"/> to suspend.</param>
		void SuspendProcess(global::System.Diagnostics.Process process);

		/// <summary>
		/// Resume a given suspended <see cref="Process"/>.
		/// </summary>
		/// <param name="process">The <see cref="Process"/> to susperesumend.</param>
		void ResumeProcess(global::System.Diagnostics.Process process);
	}
}
