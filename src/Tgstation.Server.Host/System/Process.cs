using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;

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
		/// The <see cref="global::System.Diagnostics.Process.SafeHandle"/>.
		/// </summary>
		/// <remarks>We keep this to prevent .NET from closing the real handle too soon. See https://stackoverflow.com/a/47656845</remarks>
		readonly SafeProcessHandle safeHandle;

		/// <summary>
		/// The <see cref="Task{TResult}"/> resulting in the process' standard output text.
		/// </summary>
		readonly Task<string> standardOutputTask;

		/// <summary>
		/// The <see cref="Task{TResult}"/> resulting in the process' standard error text.
		/// </summary>
		readonly Task<string> standardErrorTask;

		/// <summary>
		/// The <see cref="Task{TResult}"/> resulting in the process' unified standard output and standard error text.
		/// </summary>
		readonly StringBuilder combinedStringBuilder;

		/// <summary>
		/// Initializes a new instance of the <see cref="Process"/> class.
		/// </summary>
		/// <param name="processFeatures">The value of <see cref="processFeatures"/>.</param>
		/// <param name="handle">The value of <see cref="handle"/>.</param>
		/// <param name="lifetime">The value of <see cref="Lifetime"/>.</param>
		/// <param name="standardOutputTask">The value of <see cref="standardOutputTask"/>.</param>
		/// <param name="standardErrorTask">The value of <see cref="standardErrorTask"/>.</param>
		/// <param name="combinedStringBuilder">The value of <see cref="combinedStringBuilder"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="preExisting">If <paramref name="handle"/> was NOT just created.</param>
		public Process(
			IProcessFeatures processFeatures,
			global::System.Diagnostics.Process handle,
			Task<int> lifetime,
			Task<string> standardOutputTask,
			Task<string> standardErrorTask,
			StringBuilder combinedStringBuilder,
			ILogger<Process> logger,
			bool preExisting)
		{
			this.handle = handle ?? throw new ArgumentNullException(nameof(handle));

			// Do this fast because the runtime will bitch if we try to access it after it ends
			safeHandle = handle.SafeHandle;
			Id = handle.Id;

			this.processFeatures = processFeatures ?? throw new ArgumentNullException(nameof(processFeatures));

			this.standardOutputTask = standardOutputTask;
			this.standardErrorTask = standardErrorTask;
			this.combinedStringBuilder = combinedStringBuilder;

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			Lifetime = WrapLifetimeTask(lifetime ?? throw new ArgumentNullException(nameof(lifetime)));

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
						logger.LogDebug(ex, "Error on WaitForInputIdle()!");
					}
				},
				default, // DCT: None available
				DefaultIOManager.BlockingTaskCreationOptions,
				TaskScheduler.Current);

			logger.LogTrace("Created process ID: {0}", Id);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			safeHandle.Dispose();
			handle.Dispose();
		}

		/// <inheritdoc />
		public async Task<string> GetCombinedOutput(CancellationToken cancellationToken)
		{
			if (combinedStringBuilder == null)
				throw new InvalidOperationException("Output/Error stream reading was not enabled!");
			await Task.WhenAll(
				GetStandardOutput(cancellationToken),
				GetErrorOutput(cancellationToken))
				.ConfigureAwait(false);
			return combinedStringBuilder.ToString().TrimStart(Environment.NewLine.ToCharArray());
		}

		/// <inheritdoc />
		public async Task<string> GetErrorOutput(CancellationToken cancellationToken)
		{
			if (standardErrorTask == null)
				throw new InvalidOperationException("Error stream reading was not enabled!");
			if (!standardErrorTask.IsCompleted)
				logger.LogTrace("Waiting for PID {0} to close error stream...", Id);
			return await standardErrorTask.WithToken(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<string> GetStandardOutput(CancellationToken cancellationToken)
		{
			if (standardOutputTask == null)
				throw new InvalidOperationException("Output stream reading was not enabled!");
			if (!standardOutputTask.IsCompleted)
				logger.LogTrace("Waiting for PID {0} to close output stream...", Id);
			return await standardOutputTask.WithToken(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public void Terminate()
		{
			if (handle.HasExited)
			{
				logger.LogTrace("PID {0} already exited", Id);
				return;
			}

			try
			{
				logger.LogTrace("Terminating PID {0}...", Id);
				handle.Kill();
				if (!handle.WaitForExit(5000))
					logger.LogWarning("WaitForExit() on PID {0} timed out!", Id);
			}
			catch (Exception e)
			{
				logger.LogDebug(e, "PID {0} termination exception!", Id);
			}
		}

		/// <inheritdoc />
		public void AdjustPriority(bool higher)
		{
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
		public void Suspend()
		{
			try
			{
				processFeatures.SuspendProcess(handle);
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
				processFeatures.ResumeProcess(handle);
				logger.LogTrace("Resumed PID {0}", Id);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to resume PID {0}!", Id);
				throw;
			}
		}

		/// <inheritdoc />
		public async Task<string> GetExecutingUsername(CancellationToken cancellationToken)
		{
			var result = await processFeatures.GetExecutingUsername(handle, cancellationToken).ConfigureAwait(false);
			logger.LogTrace("PID {0} Username: {1}", Id, result);
			return result;
		}

		/// <inheritdoc />
		public Task CreateDump(string outputFile, CancellationToken cancellationToken)
		{
			if (outputFile == null)
				throw new ArgumentNullException(nameof(outputFile));

			logger.LogTrace("Dumping PID {0} to {1}...", Id, outputFile);
			return processFeatures.CreateDump(handle, outputFile, cancellationToken);
		}

		/// <summary>
		/// Attaches a log message to the process' exit event.
		/// </summary>
		/// <param name="lifetimeTask">The original lifetime <see cref="Task{TResult}"/>.</param>
		/// <returns>A <see cref="Task{TResult}"/> functionally identical to <paramref name="lifetimeTask"/>.</returns>
		async Task<int> WrapLifetimeTask(Task<int> lifetimeTask)
		{
			var exitCode = await lifetimeTask.ConfigureAwait(false);
			logger.LogTrace("PID {0} exited with code {1}", Id, exitCode);
			return exitCode;
		}
	}
}
