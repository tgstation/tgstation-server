using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

using BetterWin32Errors;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	sealed class WindowsProcessFeatures : IProcessFeatures
	{
		/// <inheritdoc />
		public void ResumeProcess(global::System.Diagnostics.Process process)
		{
			if (process == null)
				throw new ArgumentNullException(nameof(process));

			foreach (ProcessThread thread in process.Threads)
			{
				var pOpenThread = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SuspendResume, false, (uint)thread.Id);
				if (pOpenThread == IntPtr.Zero)
					continue;

				try
				{
					if (NativeMethods.ResumeThread(pOpenThread) == UInt32.MaxValue)
						throw new Win32Exception();
				}
				finally
				{
					NativeMethods.CloseHandle(pOpenThread);
				}
			}
		}

		/// <inheritdoc />
		public void SuspendProcess(global::System.Diagnostics.Process process)
		{
			if (process == null)
				throw new ArgumentNullException(nameof(process));

			foreach (ProcessThread thread in process.Threads)
			{
				var pOpenThread = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SuspendResume, false, (uint)thread.Id);
				if (pOpenThread == IntPtr.Zero)
					continue;
				try
				{
					if (NativeMethods.SuspendThread(pOpenThread) == UInt32.MaxValue)
						throw new Win32Exception();
				}
				finally
				{
					NativeMethods.CloseHandle(pOpenThread);
				}
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
		public Task CreateDump(global::System.Diagnostics.Process process, string outputFile, CancellationToken cancellationToken)
			=> Task.Factory.StartNew(
				() =>
				{
					try
					{
						if (process.HasExited)
							throw new JobException(ErrorCode.DreamDaemonOffline);
					}
					catch (InvalidOperationException ex)
					{
						throw new JobException(ErrorCode.DreamDaemonOffline, ex);
					}

					using var fileStream = new FileStream(outputFile, FileMode.CreateNew);
					if (!NativeMethods.MiniDumpWriteDump(
						process.Handle,
						(uint)process.Id,
						fileStream.SafeFileHandle,
						NativeMethods.MiniDumpType.WithDataSegs
						| NativeMethods.MiniDumpType.WithFullMemory
						| NativeMethods.MiniDumpType.WithHandleData
						| NativeMethods.MiniDumpType.WithThreadInfo
						| NativeMethods.MiniDumpType.WithUnloadedModules,
						IntPtr.Zero,
						IntPtr.Zero,
						IntPtr.Zero))
						throw new Win32Exception();
				},
				cancellationToken,
				DefaultIOManager.BlockingTaskCreationOptions,
				TaskScheduler.Current);
	}
}
