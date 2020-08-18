using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
		/// The <see cref="ILogger"/> for the <see cref="Process"/>
		/// </summary>
		readonly ILogger<Process> logger;

		readonly global::System.Diagnostics.Process handle;

		readonly SafeProcessHandle safeHandle;

		readonly Task<string> standardOutputTask;
		readonly Task<string> standardErrorTask;
		readonly StringBuilder combinedStringBuilder;

		/// <summary>
		/// Construct a <see cref="Process"/>
		/// </summary>
		/// <param name="processFeatures">The value of <see cref="processFeatures"/></param>
		/// <param name="handle">The value of <see cref="handle"/></param>
		/// <param name="lifetime">The value of <see cref="Lifetime"/></param>
		/// <param name="standardOutputTask">The value of <see cref="standardOutputTask"/></param>
		/// <param name="standardErrorTask">The value of <see cref="standardErrorTask"/></param>
		/// <param name="combinedStringBuilder">The value of <see cref="combinedStringBuilder"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="preExisting">If <paramref name="handle"/> was NOT just created</param>
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
			Id = handle.Id;

			// https://stackoverflow.com/a/47656845
			safeHandle = handle.SafeHandle;

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

		async Task<int> WrapLifetimeTask(Task<int> lifetimeTask)
		{
			var exitCode = await lifetimeTask.ConfigureAwait(false);
			logger.LogTrace("PID {0} exited with code {1}", Id, exitCode);
			return exitCode;
		}

		/// <inheritdoc />
		public async Task<string> GetCombinedOutput(CancellationToken cancellationToken)
		{
			if (combinedStringBuilder == null)
				throw new InvalidOperationException("Output/Error stream reading was not enabled!");
			await Task.WhenAll(standardOutputTask, standardErrorTask).WithToken(cancellationToken).ConfigureAwait(false);
			return combinedStringBuilder.ToString().TrimStart(Environment.NewLine.ToCharArray());
		}

		/// <inheritdoc />
		public Task<string> GetErrorOutput(CancellationToken cancellationToken)
		{
			if (standardErrorTask == null)
				throw new InvalidOperationException("Error stream reading was not enabled!");
			return standardErrorTask.WithToken(cancellationToken);
		}

		/// <inheritdoc />
		public Task<string> GetStandardOutput(CancellationToken cancellationToken)
		{
			if (standardOutputTask == null)
				throw new InvalidOperationException("Output stream reading was not enabled!");
			return standardOutputTask.WithToken(cancellationToken);
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
		public void SetHighPriority()
		{
			try
			{
				handle.PriorityClass = ProcessPriorityClass.AboveNormal;
				logger.LogTrace("Set PID {0} to above normal priority", Id);
			}
			catch (Exception e)
			{
				logger.LogWarning(e, "Unable to raise process priority for PID {0}!", Id);
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
	}
}
