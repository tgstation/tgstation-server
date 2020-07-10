using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
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

		/// <summary>
		/// The <see cref="IProcessFeatures"/> for the <see cref="Process"/>.
		/// </summary>
		readonly IProcessFeatures processFeatures;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Process"/>
		/// </summary>
		readonly ILogger<Process> logger;

		readonly global::System.Diagnostics.Process handle;

		readonly StringBuilder outputStringBuilder;
		readonly StringBuilder errorStringBuilder;
		readonly StringBuilder combinedStringBuilder;

		/// <summary>
		/// Construct a <see cref="Process"/>
		/// </summary>
		/// <param name="processFeatures">The value of <see cref="processFeatures"/></param>
		/// <param name="handle">The value of <see cref="handle"/></param>
		/// <param name="lifetime">The value of <see cref="Lifetime"/></param>
		/// <param name="outputStringBuilder">The value of <see cref="outputStringBuilder"/></param>
		/// <param name="errorStringBuilder">The value of <see cref="errorStringBuilder"/></param>
		/// <param name="combinedStringBuilder">The value of <see cref="combinedStringBuilder"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="preExisting">If <paramref name="handle"/> was NOT just created</param>
		public Process(
			IProcessFeatures processFeatures,
			global::System.Diagnostics.Process handle,
			Task<int> lifetime,
			StringBuilder outputStringBuilder,
			StringBuilder errorStringBuilder,
			StringBuilder combinedStringBuilder,
			ILogger<Process> logger,
			bool preExisting)
		{
			this.processFeatures = processFeatures ?? throw new ArgumentNullException(nameof(processFeatures));
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
			return outputStringBuilder.ToString().TrimStart(Environment.NewLine.ToCharArray());
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
		public void Suspend() => processFeatures.SuspendProcess(handle);

		/// <inheritdoc />
		public void Resume() => processFeatures.ResumeProcess(handle);

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
