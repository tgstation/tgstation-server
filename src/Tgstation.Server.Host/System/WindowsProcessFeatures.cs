using BetterWin32Errors;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	sealed class WindowsProcessFeatures : IProcessFeatures
	{
		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="WindowsProcessFeatures"/>.
		/// </summary>
		readonly ILogger<WindowsProcessFeatures> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowsProcessFeatures"/> <see langword="class"/>.
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public WindowsProcessFeatures(ILogger<WindowsProcessFeatures> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public void ResumeProcess(global::System.Diagnostics.Process process)
		{
			if (process == null)
				throw new ArgumentNullException(nameof(process));

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
			if (process == null)
				throw new ArgumentNullException(nameof(process));

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

		/// <inheritdoc />
		public Task<string> GetExecutingUsername(global::System.Diagnostics.Process process, CancellationToken cancellationToken)
		{
			string query = $"SELECT * FROM Win32_Process WHERE ProcessId = {process?.Id ?? throw new ArgumentNullException(nameof(process))}";
			using var searcher = new ManagementObjectSearcher(query);
			foreach (ManagementObject obj in searcher.Get())
			{
				var argList = new string[] { String.Empty, String.Empty };
				var returnString = obj.InvokeMethod(
					"GetOwner",
					argList)
					?.ToString();

				if (!Int32.TryParse(returnString, out var returnVal))
					return Task.FromResult($"BAD RETURN PARSE: {returnString}");

				if (returnVal == 0)
				{
					// return DOMAIN\user
					string owner = argList.Last() + "\\" + argList.First();
					return Task.FromResult(owner);
				}
			}

			return Task.FromResult("NO OWNER");
		}

		/// <inheritdoc />
		public async Task CreateDump(global::System.Diagnostics.Process process, string outputFile, CancellationToken cancellationToken)
		{
			await Task.Factory.StartNew(
				() =>
				{
					using var fileStream = new FileStream(outputFile, FileMode.CreateNew);
					if (!NativeMethods.MiniDumpWriteDump(
						process.Handle,
						(uint)process.Id,
						fileStream.SafeFileHandle,
						NativeMethods.MiniDumpType.Normal,
						IntPtr.Zero,
						IntPtr.Zero,
						IntPtr.Zero))
						throw new Win32Exception();
				},
				cancellationToken,
				TaskCreationOptions.LongRunning,
				TaskScheduler.Current)
				.ConfigureAwait(false);
		}
	}
}
