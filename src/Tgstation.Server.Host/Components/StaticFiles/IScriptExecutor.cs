using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.StaticFiles
{
	/// <summary>
	/// For executing system shell scripts
	/// </summary>
	interface IScriptExecutor
	{
		/// <summary>
		/// Execute a shell script and get the result
		/// </summary>
		/// <param name="scriptPath">The absolute path to the script</param>
		/// <param name="parameters">Command line parameters for the script</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="System.Diagnostics.Process.ExitCode"/> or <see langword="null"/> if an error occurred</returns>
		Task<int?> ExecuteScript(string scriptPath, IEnumerable<string> parameters, CancellationToken cancellationToken);
	}
}
