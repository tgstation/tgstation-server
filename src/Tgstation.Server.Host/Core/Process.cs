using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Core
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

		readonly System.Diagnostics.Process handle;

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
		public Process(System.Diagnostics.Process handle, Task<int> lifetime, StringBuilder outputStringBuilder, StringBuilder errorStringBuilder, StringBuilder combinedStringBuilder, ILogger<Process> logger, bool preExisting)
		{
			this.handle = handle ?? throw new ArgumentNullException(nameof(handle));
			Lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));

			this.outputStringBuilder = outputStringBuilder;
			this.errorStringBuilder = errorStringBuilder;
			this.combinedStringBuilder = combinedStringBuilder;

			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
				logger.LogTrace("Terminating process...");
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
				handle.PriorityClass = System.Diagnostics.ProcessPriorityClass.AboveNormal;
				logger.LogTrace("Set to above normal priority", handle.Id);
			}
			catch (Exception e)
			{
				logger.LogWarning("Unable to raise process priority! Exception: {0}", e);
			}
		}
	}
}
