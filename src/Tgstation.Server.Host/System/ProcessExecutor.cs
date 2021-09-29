using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

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
		/// The <see cref="ILogger"/> for the <see cref="ProcessExecutor"/>.
		/// </summary>
		readonly ILogger<ProcessExecutor> logger;

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="ProcessExecutor"/>.
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// Wrapper for <see cref="AttachExitHandler(global::System.Diagnostics.Process, Func{int})"/> to safely provide the process ID.
		/// </summary>
		/// <param name="handle">The <see cref="global::System.Diagnostics.Process"/> to attach an exit handler to.</param>
		/// <param name="startupTask">A <see cref="Task"/> that completes once the process represented by <paramref name="handle"/> launches.</param>
		/// <returns>The result of the call to <see cref="AttachExitHandler(global::System.Diagnostics.Process, Func{int})"/>.</returns>
		async Task<Task<int>> AttachExitHandlerBeforeLaunch(global::System.Diagnostics.Process handle, Task startupTask)
		{
			var id = -1;
			var result = AttachExitHandler(handle, () => id);
			await startupTask.ConfigureAwait(false);
			id = handle.Id;
			return result;
		}

		/// <summary>
		/// Attach an asychronous exit handler to a given process <paramref name="handle"/>.
		/// </summary>
		/// <param name="handle">The <see cref="global::System.Diagnostics.Process"/> to attach an exit handler to.</param>
		/// <param name="idProvider">A <see cref="Func{TResult}"/> that can be called to get the <see cref="global::System.Diagnostics.Process.Id"/> safely.</param>
		/// <returns>A <see cref="Task{TResult}"/> that completes with the exit code of the process represented by <paramref name="handle"/>.</returns>
		Task<int> AttachExitHandler(global::System.Diagnostics.Process handle, Func<int> idProvider)
		{
			handle.EnableRaisingEvents = true;

			var tcs = new TaskCompletionSource<int>();
			void ExitHandler(object? sender, EventArgs args)
			{
				var id = idProvider();
				try
				{
					if (tcs.Task.IsCompleted)
					{
						logger.LogTrace("Skipping PID {0} exit handler as the TaskCompletionSource is already set", id);
						return;
					}

					try
					{
						var exitCode = handle.ExitCode;

						// Try because this can be invoked twice for weird reasons
						if (tcs.TrySetResult(exitCode))
							logger.LogTrace("PID {0} termination event completed", id);
						else
							logger.LogTrace("Ignoring duplicate PID {0} termination event", id);
					}
					catch (InvalidOperationException ex)
					{
						if (!tcs.Task.IsCompleted)
							throw;

						logger.LogTrace(ex, "Ignoring expected PID {0} exit handler exception!", id);
					}
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "PID {0} exit handler exception!", id);
				}
			}

			handle.Exited += ExitHandler;

			return tcs.Task;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ProcessExecutor"/> class.
		/// </summary>
		/// <param name="processFeatures">The value of <see cref="processFeatures"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/>.</param>
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
		public IProcess? GetProcess(int id)
		{
			logger.LogDebug("Attaching to process {0}...", id);
			global::System.Diagnostics.Process handle;
			try
			{
				handle = global::System.Diagnostics.Process.GetProcessById(id);
			}
			catch (Exception e)
			{
				logger.LogDebug(e, "Unable to get process {0}!", id);
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
			string? arguments,
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

				StringBuilder? combinedStringBuilder = null;

				Task<string>? outputTask = null;
				Task<string>? errorTask = null;
				var processStartTcs = new TaskCompletionSource();
				if (readOutput || readError)
				{
					combinedStringBuilder = new StringBuilder();

					async Task<string> ConsumeReader(Func<TextReader> readerFunc, bool isOutputStream)
					{
						var stringBuilder = new StringBuilder();

						await processStartTcs.Task.ConfigureAwait(false);

						var pid = handle.Id;
						var streamType = isOutputStream ? "out" : "err";
						logger.LogTrace("Starting std{0} read for PID {1}...", streamType, pid);

						var reader = readerFunc();
						string? text;
						while ((text = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
						{
							combinedStringBuilder.AppendLine();
							combinedStringBuilder.Append(text);
							stringBuilder.AppendLine();
							stringBuilder.Append(text);
						}

						logger.LogTrace("Finished std{0} read for PID {1}", streamType, pid);

						return stringBuilder.ToString();
					}

					if (readOutput)
					{
						outputTask = ConsumeReader(() => handle.StandardOutput, true);
						handle.StartInfo.RedirectStandardOutput = true;
					}

					if (readError)
					{
						errorTask = ConsumeReader(() => handle.StandardError, false);
						handle.StartInfo.RedirectStandardError = true;
					}
				}

				var lifetimeTaskTask = AttachExitHandlerBeforeLaunch(handle, processStartTcs.Task);

				try
				{
					handle.Start();

					processStartTcs.SetResult();
				}
				catch (Exception ex)
				{
					processStartTcs.SetException(ex);
					throw;
				}

				var process = new Process(
					processFeatures,
					handle,
					lifetimeTaskTask.GetAwaiter().GetResult(), // won't block
					outputTask,
					errorTask,
					combinedStringBuilder,
					loggerFactory.CreateLogger<Process>(),
					false);

				return process;
			}
			catch
			{
				handle.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public IProcess? GetProcessByName(string name)
		{
			logger.LogTrace("GetProcessByName: {0}...", name ?? throw new ArgumentNullException(nameof(name)));
			var procs = global::System.Diagnostics.Process.GetProcessesByName(name);
			global::System.Diagnostics.Process? handle = null;
			foreach (var proc in procs)
				if (handle == null)
					handle = proc;
				else
				{
					logger.LogTrace("Disposing extra found PID: {0}...", proc.Id);
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
				var pid = handle.Id;
				return new Process(
					processFeatures,
					handle,
					AttachExitHandler(handle, () => pid),
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
