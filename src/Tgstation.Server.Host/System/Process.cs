using BetterWin32Errors;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	sealed class Process : IProcess
	{
		/// <inheritdoc />
		public int Id { get; }

		/// <inheritdoc />
		public Task Startup { get; }

		/// <inheritdoc />
		public Task<int> Lifetime { get; }

		readonly global::System.Diagnostics.Process handle;

		readonly StringBuilder outputStringBuilder;
		readonly StringBuilder errorStringBuilder;
		readonly StringBuilder combinedStringBuilder;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Process"/>
		/// </summary>
		readonly ILogger<Process> logger;

		/// <summary>
		/// Construct a <see cref="Process"/>
		/// </summary>
		/// <param name="handle">The value of <see cref="handle"/></param>
		/// <param name="lifetime">The value of <see cref="Lifetime"/></param>
		/// <param name="outputStringBuilder">The value of <see cref="outputStringBuilder"/></param>
		/// <param name="errorStringBuilder">The value of <see cref="errorStringBuilder"/></param>
		/// <param name="combinedStringBuilder">The value of <see cref="combinedStringBuilder"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="preExisting">If <paramref name="handle"/> was NOT just created</param>
		public Process(
			global::System.Diagnostics.Process handle,
			Task<int> lifetime,
			StringBuilder outputStringBuilder,
			StringBuilder errorStringBuilder,
			StringBuilder combinedStringBuilder,
			ILogger<Process> logger,
			bool preExisting)
		{
			this.handle = handle ?? throw new ArgumentNullException(nameof(handle));

			this.outputStringBuilder = outputStringBuilder;
			this.errorStringBuilder = errorStringBuilder;
			this.combinedStringBuilder = combinedStringBuilder;

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			Lifetime = WrapLifetimeTask(lifetime ?? throw new ArgumentNullException(nameof(lifetime)));

			Id = handle.Id;

			if (preExisting)
			{
				Startup = Task.CompletedTask;
				return;
			}

			Startup = Task.Factory.StartNew(() =>
			{
				try
				{
					handle.WaitForInputIdle();
				}
				catch (InvalidOperationException) { }
			}, default, TaskCreationOptions.LongRunning, TaskScheduler.Current);

			logger.LogTrace("Created process ID: {0}", Id);
		}

		/// <inheritdoc />
		public void Dispose() => handle.Dispose();

		async Task<int> WrapLifetimeTask(Task<int> lifetimeTask)
		{
			var result = await lifetimeTask.ConfigureAwait(false);
			logger.LogTrace("PID {0} ended with code {1}", Id, result);
			return result;
		}

		/// <inheritdoc />
		public string GetCombinedOutput()
		{
			if (combinedStringBuilder == null)
				throw new InvalidOperationException("Output/Error reading was not enabled!");
			return combinedStringBuilder.ToString().TrimStart(Environment.NewLine.ToCharArray());
		}

		/// <inheritdoc />
		public string GetErrorOutput()
		{
			if (errorStringBuilder == null)
				throw new InvalidOperationException("Error reading was not enabled!");
			return errorStringBuilder.ToString().TrimStart(Environment.NewLine.ToCharArray());
		}

		/// <inheritdoc />
		public string GetStandardOutput()
		{
			if (outputStringBuilder == null)
				throw new InvalidOperationException("Output reading was not enabled!");
			return errorStringBuilder.ToString().TrimStart(Environment.NewLine.ToCharArray());
		}

		/// <inheritdoc />
		public void Terminate()
		{
			if (handle.HasExited)
				return;
			try
			{
				logger.LogTrace("Terminating PID {0}...", Id);
				handle.Kill();
				handle.WaitForExit();
			}
			catch (Exception e)
			{
				logger.LogDebug("Process termination exception: {0}", e);
			}
		}

		/// <inheritdoc />
		public void SetHighPriority()
		{
			try
			{
				handle.PriorityClass = ProcessPriorityClass.AboveNormal;
				logger.LogTrace("Set PID {0} to above normal priority", Id);
			}
			catch (Exception e)
			{
				logger.LogWarning("Unable to raise process priority for PID {0}! Exception: {1}", Id, e);
			}
		}

		/// <inheritdoc />
		public void Suspend()
		{
			try
			{
				foreach (ProcessThread thread in handle.Threads)
				{
					var pOpenThread = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SuspendResume, false, (uint)thread.Id);
					if (pOpenThread == IntPtr.Zero)
						continue;

					if (NativeMethods.SuspendThread(pOpenThread) == UInt32.MaxValue)
						throw new Win32Exception();

					NativeMethods.CloseHandle(pOpenThread);
				}

				logger.LogTrace("Suspended PID {0}", Id);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to suspend PID {0}!", Id);
				throw;
			}
		}

		/// <inheritdoc />
		public void Resume()
		{
			try
			{
				foreach (ProcessThread thread in handle.Threads)
				{
					var pOpenThread = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SuspendResume, false, (uint)thread.Id);
					if (pOpenThread == IntPtr.Zero)
						continue;

					if (NativeMethods.ResumeThread(pOpenThread) == UInt32.MaxValue)
						throw new Win32Exception();

					NativeMethods.CloseHandle(pOpenThread);
				}

				logger.LogTrace("Resumed PID {0}", Id);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to resume PID {0}!", Id);
				throw;
			}
		}
	}
}
