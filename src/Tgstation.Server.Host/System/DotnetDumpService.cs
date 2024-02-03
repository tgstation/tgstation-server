using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	sealed class DotnetDumpService : IDotnetDumpService
	{
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="DotnetDumpService"/>.
		/// </summary>
		readonly ILogger<DotnetDumpService> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="DotnetDumpService"/> class.
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public DotnetDumpService(
			ILogger<DotnetDumpService> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public async ValueTask Dump(IProcess process, string outputFile, bool minidump, CancellationToken cancellationToken)
		{
			// need to use an extra timeout here because if the process is truly deadlocked. A cooperative dump will hang forever
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			const int TimeoutMinutes = 5;
			cts.CancelAfter(TimeSpan.FromMinutes(TimeoutMinutes));
			cts.Token.Register(() =>
			{
				if (!cancellationToken.IsCancellationRequested)
					logger.LogError("dotnet-dump timed out after {minutes} minutes!", TimeoutMinutes);
			});

			var pid = process.Id;
			logger.LogDebug("dotnet-dump requested for PID {pid}...", pid);
			var client = new DiagnosticsClient(pid);
			await client.WriteDumpAsync(
				minidump
					? DumpType.Normal
					: DumpType.Full,
				outputFile,
				false,
				cts.Token);
		}
	}
}
