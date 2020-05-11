using BetterWin32Errors;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	sealed class WindowsProcessSuspender : IProcessSuspender
	{
		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="WindowsProcessSuspender"/>.
		/// </summary>
		readonly ILogger<WindowsProcessSuspender> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowsProcessSuspender"/> <see langword="class"/>.
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public WindowsProcessSuspender(ILogger<WindowsProcessSuspender> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public void ResumeProcess(global::System.Diagnostics.Process process)
		{
			try
			{
				foreach (ProcessThread thread in process.Threads)
				{
					var pOpenThread = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SuspendResume, false, (uint)thread.Id);
					if (pOpenThread == IntPtr.Zero)
						continue;

					if (NativeMethods.ResumeThread(pOpenThread) == UInt32.MaxValue)
						throw new Win32Exception();

					NativeMethods.CloseHandle(pOpenThread);
				}

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
				foreach (ProcessThread thread in process.Threads)
				{
					var pOpenThread = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SuspendResume, false, (uint)thread.Id);
					if (pOpenThread == IntPtr.Zero)
						continue;

					if (NativeMethods.SuspendThread(pOpenThread) == UInt32.MaxValue)
						throw new Win32Exception();

					NativeMethods.CloseHandle(pOpenThread);
				}

				logger.LogTrace("Suspended PID {0}", process.Id);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to suspend PID {0}!", process.Id);
				throw;
			}
		}
	}
}
