using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using BetterWin32Errors;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	[SupportedOSPlatform("windows")]
	sealed class WindowsProcessFeatures : IProcessFeatures
	{
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="WindowsProcessFeatures"/>.
		/// </summary>
		readonly ILogger<WindowsProcessFeatures> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowsProcessFeatures"/> class.
		/// </summary>
		/// <param name="logger">The value of logger.</param>
		public WindowsProcessFeatures(ILogger<WindowsProcessFeatures> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public void ResumeProcess(global::System.Diagnostics.Process process)
		{
			if (process == null)
				throw new ArgumentNullException(nameof(process));

			process.Refresh();
			foreach (ProcessThread thread in process.Threads)
			{
				var threadId = (uint)thread.Id;
				logger.LogTrace("Resuming thread {threadId}...", threadId);
				var pOpenThread = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SuspendResume, false, threadId);
				if (pOpenThread == IntPtr.Zero)
				{
					logger.LogDebug(new Win32Exception(), "Failed to open thread {threadId}!", threadId);
					continue;
				}

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

			process.Refresh();
			foreach (ProcessThread thread in process.Threads)
			{
				var threadId = (uint)thread.Id;
				logger.LogTrace("Suspending thread {threadId}...", threadId);
				var pOpenThread = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SuspendResume, false, threadId);
				if (pOpenThread == IntPtr.Zero)
				{
					logger.LogDebug(new Win32Exception(), "Failed to open thread {threadId}!", threadId);
					continue;
				}

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
			foreach (var obj in searcher.Get().Cast<ManagementObject>())
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
