using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	sealed class Process : IProcess
	{
		/// <inheritdoc />
		public int Id { get; }

		/// <inheritdoc />
		public DateTimeOffset? LaunchTime
		{
			get
			{
				try
				{
					return handle.StartTime;
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Failed to get PID {pid}'s memory usage!", Id);
					return null;
				}
			}
		}

		/// <inheritdoc />
		public Task Startup { get; }

		/// <inheritdoc />
		public Task<int?> Lifetime { get; }

		/// <inheritdoc />
		public long? MemoryUsage
		{
			get
			{
				try
				{
					return handle.PrivateMemorySize64;
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Failed to get PID {pid}'s memory usage!", Id);
					return null;
				}
			}
		}

		/// <summary>
		/// The <see cref="IProcessFeatures"/> for the <see cref="Process"/>.
		/// </summary>
		readonly IProcessFeatures processFeatures;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Process"/>.
		/// </summary>
		readonly ILogger<Process> logger;

		/// <summary>
		/// The <see cref="global::System.Diagnostics.Process"/> <see cref="object"/>.
		/// </summary>
		readonly global::System.Diagnostics.Process handle;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> used to shutdown the <see cref="readTask"/> and <see cref="Lifetime"/>.
		/// </summary>
		readonly CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// The <see cref="global::System.Diagnostics.Process.SafeHandle"/>.
		/// </summary>
		/// <remarks>We keep this to prevent .NET from closing the real handle too soon. See https://stackoverflow.com/a/47656845</remarks>
		readonly SafeProcessHandle safeHandle;

		/// <summary>
		/// The <see cref="Task{TResult}"/> resulting in the process' standard output/error text.
		/// </summary>
		readonly Task<string?>? readTask;

		/// <summary>
		/// If the <see cref="Process"/> was disposed.
		/// </summary>
		volatile int disposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="Process"/> class.
		/// </summary>
		/// <param name="processFeatures">The value of <see cref="processFeatures"/>.</param>
		/// <param name="handle">The value of <see cref="handle"/>.</param>
		/// <param name="readerCts">The override value of <see cref="cancellationTokenSource"/>.</param>
		/// <param name="readTask">The value of <see cref="readTask"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="preExisting">If <paramref name="handle"/> was NOT just created.</param>
		public Process(
			IProcessFeatures processFeatures,
			global::System.Diagnostics.Process handle,
			CancellationTokenSource? readerCts,
			Task<string?>? readTask,
			ILogger<Process> logger,
			bool preExisting)
		{
			this.handle = handle ?? throw new ArgumentNullException(nameof(handle));

			// Do this fast because the runtime will bitch if we try to access it after it ends
			safeHandle = handle.SafeHandle;
			Id = handle.Id;

			cancellationTokenSource = readerCts ?? new CancellationTokenSource();

			this.processFeatures = processFeatures ?? throw new ArgumentNullException(nameof(processFeatures));

			this.readTask = readTask;

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			Lifetime = WrapLifetimeTask();

			if (preExisting)
			{
				Startup = Task.CompletedTask;
				return;
			}

			Startup = Task.Factory.StartNew(
				() =>
				{
					try
					{
						handle.WaitForInputIdle();
					}
					catch (Exception ex)
					{
						logger.LogTrace(ex, "WaitForInputIdle() failed, this is normal.");
					}
				},
				CancellationToken.None, // DCT: None available
				DefaultIOManager.BlockingTaskCreationOptions,
				TaskScheduler.Current);

			logger.LogTrace("Created process ID: {pid}", Id);
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			if (Interlocked.Exchange(ref disposed, 1) != 0)
				return;

			logger.LogTrace("Disposing PID {pid}...", Id);
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
			if (readTask != null)
				await readTask;

			await Lifetime;

			safeHandle.Dispose();
			handle.Dispose();
		}

		/// <inheritdoc />
		public Task<string?> GetCombinedOutput(CancellationToken cancellationToken)
		{
			if (readTask == null)
				throw new InvalidOperationException("Output/Error stream reading was not enabled!");

			return readTask.WaitAsync(cancellationToken);
		}

		/// <inheritdoc />
		public void Terminate()
		{
			CheckDisposed();
			if (handle.HasExited)
			{
				logger.LogTrace("PID {pid} already exited", Id);
				return;
			}

			try
			{
				logger.LogTrace("Terminating PID {pid}...", Id);
				handle.Kill();
				if (!handle.WaitForExit(5000))
					logger.LogWarning("WaitForExit() on PID {pid} timed out!", Id);
			}
			catch (Exception e)
			{
				logger.LogDebug(e, "PID {pid} termination exception!", Id);
			}
		}

		/// <inheritdoc />
		public void AdjustPriority(bool higher)
		{
			CheckDisposed();
			var targetPriority = higher ? ProcessPriorityClass.AboveNormal : ProcessPriorityClass.BelowNormal;
			try
			{
				handle.PriorityClass = targetPriority;
				logger.LogTrace("Set PID {pid} to {targetPriority} priority", Id, targetPriority);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Unable to set priority for PID {id} to {targetPriority}!", Id, targetPriority);
			}
		}

		/// <inheritdoc />
		public void SuspendProcess()
		{
			CheckDisposed();
			try
			{
				processFeatures.SuspendProcess(handle);
				logger.LogTrace("Suspended PID {pid}", Id);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to suspend PID {pid}!", Id);
				throw;
			}
		}

		/// <inheritdoc />
		public void ResumeProcess()
		{
			CheckDisposed();
			try
			{
				processFeatures.ResumeProcess(handle);
				logger.LogTrace("Resumed PID {pid}", Id);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to resume PID {pid}!", Id);
				throw;
			}
		}

		/// <inheritdoc />
		public string GetExecutingUsername()
		{
			CheckDisposed();
			var result = processFeatures.GetExecutingUsername(handle);
			logger.LogTrace("PID {pid} Username: {username}", Id, result);
			return result;
		}

		/// <inheritdoc />
		public ValueTask CreateDump(string outputFile, bool minidump, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(outputFile);
			CheckDisposed();

			logger.LogTrace("Dumping PID {pid} to {dumpFilePath}...", Id, outputFile);
			return processFeatures.CreateDump(handle, outputFile, minidump, cancellationToken);
		}

		/// <summary>
		/// Attaches a log message to the process' exit event.
		/// </summary>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="global::System.Diagnostics.Process.ExitCode"/> or <see langword="null"/> if the process was detached.</returns>
		async Task<int?> WrapLifetimeTask()
		{
			bool hasExited;
			try
			{
				await handle.WaitForExitAsync(cancellationTokenSource.Token);
				hasExited = true;
			}
			catch (OperationCanceledException ex)
			{
				logger.LogTrace(ex, "Process lifetime task cancelled!");
				hasExited = handle.HasExited;
			}

			if (!hasExited)
				return null;

			var exitCode = handle.ExitCode;
			logger.LogTrace("PID {pid} exited with code {exitCode}", Id, exitCode);
			return exitCode;
		}

		/// <summary>
		/// Throws an <see cref="ObjectDisposedException"/> if a method of the <see cref="Process"/> was called after <see cref="DisposeAsync"/>.
		/// </summary>
		void CheckDisposed() => ObjectDisposedException.ThrowIf(disposed != 0, this);
	}
}
