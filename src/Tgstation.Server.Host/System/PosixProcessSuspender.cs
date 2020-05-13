using Microsoft.Extensions.Logging;
using Mono.Unix;
using Mono.Unix.Native;
using System;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	sealed class PosixProcessSuspender : IProcessSuspender
	{
		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="PosixProcessSuspender"/>.
		/// </summary>
		readonly ILogger<PosixProcessSuspender> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="PosixProcessSuspender"/> <see langword="class"/>.
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public PosixProcessSuspender(ILogger<PosixProcessSuspender> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public void ResumeProcess(global::System.Diagnostics.Process process)
		{
			try
			{
				var result = Syscall.kill(process.Id, Signum.SIGCONT);
				if (result != 0)
					throw new UnixIOException(result);
				logger.LogTrace("Resumed PID {0}", process.Id);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to resume PID {0}!", process.Id);
				throw;
			}
		}

		/// <inheritdoc />
		public void SuspendProcess(global::System.Diagnostics.Process process)
		{
			try
			{
				var result = Syscall.kill(process.Id, Signum.SIGSTOP);
				if (result != 0)
					throw new UnixIOException(result);
				logger.LogTrace("Resumed PID {0}", process.Id);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to suspend PID {0}!", process.Id);
				throw;
			}
		}
	}
}
