using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// Helper <see langword="class"/> for interacting with the Windows Firewall.
	/// </summary>
	static class WindowsFirewallHelper
	{
		/// <summary>
		/// Add an executable exception to the Windows firewall.
		/// </summary>
		/// <param name="processExecutor">The <see cref="IProcessExecutor"/> to use.</param>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="exceptionName">The name of the rule in Windows Firewall.</param>
		/// <param name="exePath">The path to the .exe to add a firewall exception for.</param>
		/// <param name="lowPriority">If the "netsh.exe" process should be run with lower process priority.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the exit code of the call to netsh.exe.</returns>
		public static async ValueTask<int> AddFirewallException(
			IProcessExecutor processExecutor,
			ILogger logger,
			string exceptionName,
			string exePath,
			bool lowPriority,
			CancellationToken cancellationToken)
		{
			logger.LogInformation("Adding Windows Firewall exception for {path}...", exePath);
			var arguments = $"advfirewall firewall add rule name=\"{exceptionName}\" program=\"{exePath}\" protocol=tcp dir=in enable=yes action=allow";
			await using var netshProcess = await processExecutor.LaunchProcess(
				"netsh.exe",
				Environment.CurrentDirectory,
				arguments,
				cancellationToken,
				readStandardHandles: true,
				noShellExecute: true);

			if (lowPriority)
				netshProcess.AdjustPriority(false);

			int exitCode;
			using (cancellationToken.Register(() => netshProcess.Terminate()))
				exitCode = (await netshProcess.Lifetime).Value;
			cancellationToken.ThrowIfCancellationRequested();

			logger.LogDebug(
				"netsh.exe output:{newLine}{output}",
				Environment.NewLine,
				await netshProcess.GetCombinedOutput(cancellationToken));

			return exitCode;
		}
	}
}
