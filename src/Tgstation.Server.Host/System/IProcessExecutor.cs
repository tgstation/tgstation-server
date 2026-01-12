using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// For launching <see cref="IProcess"/>'.
	/// </summary>
	interface IProcessExecutor
	{
		/// <summary>
		/// Launch a <see cref="IProcess"/>.
		/// </summary>
		/// <param name="fileName">The full path to the executable file.</param>
		/// <param name="workingDirectory">The working directory for the <see cref="IProcess"/>.</param>
		/// <param name="arguments">The arguments for the <see cref="IProcess"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <param name="environment">A <see cref="IReadOnlyDictionary{TKey, TValue}"/> of environment variables to set.</param>
		/// <param name="fileRedirect">File to write process output and error streams to. Requires <paramref name="readStandardHandles"/> to be <see langword="true"/>.</param>
		/// <param name="readStandardHandles">If the process output and error streams should be read.</param>
		/// <param name="noShellExecute">If shell execute should not be used. Must be set if <paramref name="readStandardHandles"/> is set.</param>
		/// <param name="doNotLogArguments">If <paramref name="arguments"/> should not be logged.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the new <see cref="IProcess"/>.</returns>
		ValueTask<IProcess> LaunchProcess(
			string fileName,
			string workingDirectory,
			string arguments,
			CancellationToken cancellationToken,
			IReadOnlyDictionary<string, string>? environment = null,
			string? fileRedirect = null,
			bool readStandardHandles = false,
			bool noShellExecute = false,
			bool doNotLogArguments = false);

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
		IProcess? GetProcess(int id);

		/// <summary>
		/// Get a <see cref="IProcess"/> with a given <paramref name="name"/>.
		/// </summary>
		/// <param name="name">The name of the process executable without the extension.</param>
		/// <returns>The <see cref="IProcess"/> represented by <paramref name="name"/> on success, <see langword="null"/> on failure.</returns>
		IProcess? GetProcessByName(string name);
	}
}
