namespace Tgstation.Server.Host.Common
{
	/// <summary>
	/// Represents the exit code of the <see cref="Host"/> program.
	/// </summary>
	public enum HostExitCode
	{
		/// <summary>
		/// The program ran to completion and should not be re-executed.
		/// </summary>
		CompleteExecution,

		/// <summary>
		/// The program should be re-executed after applying pending updates.
		/// </summary>
		RestartRequested,

		/// <summary>
		/// The program errored and error data was writted to the pending update path as a file.
		/// </summary>
		Error,
	}
}
