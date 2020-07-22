using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	sealed class ProcessExecutor : IProcessExecutor
	{
		/// <summary>
		/// The <see cref="IProcessFeatures"/> for the <see cref="ProcessExecutor"/>.
		/// </summary>
		readonly IProcessFeatures processFeatures;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ProcessExecutor"/>
		/// </summary>
		readonly ILogger<ProcessExecutor> logger;

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="ProcessExecutor"/>
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// Create a <see cref="Task{TResult}"/> resulting in the exit code of a given <paramref name="handle"/>
		/// </summary>
		/// <param name="handle">The <see cref="global::System.Diagnostics.Process"/> to attach the <see cref="Task{TResult}"/> for</param>
		/// <returns>A new <see cref="Task{TResult}"/> resulting in the exit code of <paramref name="handle"/></returns>
		Task<int> AttachExitHandler(global::System.Diagnostics.Process handle)
		{
			handle.EnableRaisingEvents = true;
			var tcs = new TaskCompletionSource<int>();
			handle.Exited += (a, b) =>
			{
				int exitCode;
				try
				{
					exitCode = handle.ExitCode;
				}
				catch (InvalidOperationException)
				{
					return;
				}

				// Try because this can be invoked twice for weird reasons
				if (tcs.TrySetResult(exitCode))
					logger.LogTrace("Process exit event completed");
			};

			return tcs.Task;
		}

		/// <summary>
		/// Construct a <see cref="ProcessExecutor"/>
		/// </summary>
		/// <param name="processFeatures">The value of <see cref="processFeatures"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/></param>
		public ProcessExecutor(
			IProcessFeatures processFeatures,
			ILogger<ProcessExecutor> logger,
			ILoggerFactory loggerFactory)
		{
			this.processFeatures = processFeatures ?? throw new ArgumentNullException(nameof(processFeatures));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
		}

		/// <inheritdoc />
		public IProcess GetProcess(int id)
		{
			logger.LogDebug("Attaching to process {0}...", id);
			global::System.Diagnostics.Process handle;
			try
			{
				handle = global::System.Diagnostics.Process.GetProcessById(id);
			}
			catch(Exception e)
			{
				logger.LogDebug("Unable to get process {0}! Exception: {1}", id, e);
				return null;
			}

			return CreateFromExistingHandle(handle);
		}

		/// <inheritdoc />
		public IProcess GetCurrentProcess()
		{
			logger.LogTrace("Getting current process...");
			var handle = global::System.Diagnostics.Process.GetCurrentProcess();
			return CreateFromExistingHandle(handle);
		}

		/// <inheritdoc />
		public IProcess LaunchProcess(
			string fileName,
			string workingDirectory,
			string arguments,
			bool readOutput,
			bool readError,
			bool noShellExecute)
		{
			if (fileName == null)
				throw new ArgumentNullException(nameof(fileName));
			if (workingDirectory == null)
				throw new ArgumentNullException(nameof(workingDirectory));
			if (arguments == null)
				throw new ArgumentNullException(nameof(arguments));

			if (!noShellExecute && (readOutput || readError))
				throw new InvalidOperationException("Requesting output/error reading requires noShellExecute to be true!");

			logger.LogDebug(
				"{0}aunching process in {1}: {2} {3}",
				noShellExecute ? "L" : "Shell l",
				workingDirectory,
				fileName,
				arguments);
			var handle = new global::System.Diagnostics.Process();
			try
			{
				handle.StartInfo.FileName = fileName;
				handle.StartInfo.Arguments = arguments;
				handle.StartInfo.WorkingDirectory = workingDirectory;

				handle.StartInfo.UseShellExecute = !noShellExecute;

				StringBuilder outputStringBuilder = null, errorStringBuilder = null, combinedStringBuilder = null;

				TaskCompletionSource<object> outputReadTcs = null;
				TaskCompletionSource<object> errorReadTcs = null;
				if (readOutput || readError)
				{
					combinedStringBuilder = new StringBuilder();
					if (readOutput)
					{
						outputStringBuilder = new StringBuilder();
						handle.StartInfo.RedirectStandardOutput = true;
						outputReadTcs = new TaskCompletionSource<object>();
						handle.OutputDataReceived += (sender, e) =>
						{
							if (e.Data == null)
							{
								outputReadTcs.SetResult(null);
								return;
							}

							combinedStringBuilder.Append(Environment.NewLine);
							combinedStringBuilder.Append(e.Data);
							outputStringBuilder.Append(Environment.NewLine);
							outputStringBuilder.Append(e.Data);
						};
					}

					if (readError)
					{
						errorStringBuilder = new StringBuilder();
						handle.StartInfo.RedirectStandardError = true;
						errorReadTcs = new TaskCompletionSource<object>();
						handle.ErrorDataReceived += (sender, e) =>
						{
							if (e.Data == null)
							{
								errorReadTcs.SetResult(null);
								return;
							}

							combinedStringBuilder.Append(Environment.NewLine);
							combinedStringBuilder.Append(e.Data);
							errorStringBuilder.Append(Environment.NewLine);
							errorStringBuilder.Append(e.Data);
						};
					}
				}

				var lifetimeTask = AttachExitHandler(handle);

				handle.Start();

				static async Task<int> AddToLifetimeTask(Task<int> originalTask, TaskCompletionSource<object> tcs)
				{
					var exitCode = await originalTask.ConfigureAwait(false);
					await tcs.Task.ConfigureAwait(false);
					return exitCode;
				}

				try
				{
					if (readOutput)
					{
						handle.BeginOutputReadLine();
						lifetimeTask = AddToLifetimeTask(lifetimeTask, outputReadTcs);
					}
				}
				catch (InvalidOperationException) { }
				try
				{
					if (readError)
					{
						handle.BeginErrorReadLine();
						lifetimeTask = AddToLifetimeTask(lifetimeTask, errorReadTcs);
					}
				}
				catch (InvalidOperationException) { }

				return new Process(
					processFeatures,
					handle,
					lifetimeTask,
					outputStringBuilder,
					errorStringBuilder,
					combinedStringBuilder,
					loggerFactory.CreateLogger<Process>(), false);
			}
			catch
			{
				handle.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public IProcess GetProcessByName(string name)
		{
			logger.LogTrace("GetProcessByName: {0}...", name ?? throw new ArgumentNullException(nameof(name)));
			var procs = global::System.Diagnostics.Process.GetProcessesByName(name);
			global::System.Diagnostics.Process handle = null;
			foreach (var proc in procs)
				if (handle == null)
					handle = proc;
				else
				{
					logger.LogTrace("Disposing extra found PID: {0}", proc.Id);
					proc.Dispose();
				}

			if (handle == null)
				return null;

			return CreateFromExistingHandle(handle);
		}

		/// <summary>
		/// Create a <see cref="IProcess"/> given an existing <paramref name="handle"/>.
		/// </summary>
		/// <param name="handle">The <see cref="global::System.Diagnostics.Process"/> to create a <see cref="IProcess"/> from.</param>
		/// <returns>The <see cref="IProcess"/> based on <paramref name="handle"/>.</returns>
		private IProcess CreateFromExistingHandle(global::System.Diagnostics.Process handle)
		{
			try
			{
				return new Process(
					processFeatures,
					handle,
					AttachExitHandler(handle),
					null,
					null,
					null,
					loggerFactory.CreateLogger<Process>(),
					true);
			}
			catch
			{
				handle.Dispose();
				throw;
			}
		}
	}
}
