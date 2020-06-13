namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// For launching <see cref="IProcess"/>'
	/// </summary>
	interface IProcessExecutor
	{
		/// <summary>
		/// Launch a <see cref="IProcess"/>
		/// </summary>
		/// <param name="fileName">The full path to the executable file</param>
		/// <param name="workingDirectory">The working directory for the <see cref="IProcess"/></param>
		/// <param name="arguments">The arguments for the <see cref="IProcess"/></param>
		/// <param name="readOutput">If standard output should be read</param>
		/// <param name="readError">If standard error should be read</param>
		/// <param name="noShellExecute">If shell execute should not be used. Must be set if <paramref name="readError"/> or <paramref name="readOutput"/> are set.</param>
		/// <returns>A new <see cref="IProcess"/></returns>
		IProcess LaunchProcess(string fileName, string workingDirectory, string arguments = null, bool readOutput = false, bool readError = false, bool noShellExecute = false);

		/// <summary>
		/// Get a <see cref="IProcess"/> representing the running executable.
		/// </summary>
		/// <returns>The current <see cref="IProcess"/>.</returns>
		IProcess GetCurrentProcess();

		/// <summary>
		/// Get a <see cref="IProcess"/> by <paramref name="id"/>.
		/// </summary>
		/// <param name="id">The <see cref="IProcess.Id"/>.</param>
		/// <returns>The <see cref="IProcess"/> represented by <paramref name="id"/> on success, <see langword="null"/> on failure.</returns>
		IProcess GetProcess(int id);

		/// <summary>
		/// Get a <see cref="IProcess"/> with a given <paramref name="name"/>.
		/// </summary>
		/// <param name="name">The name of the process executable without the extension.</param>
		/// <returns>The <see cref="IProcess"/> represented by <paramref name="name"/> on success, <see langword="null"/> on failure.</returns>
		IProcess GetProcessByName(string name);
	}
}
