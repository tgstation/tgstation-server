using Microsoft.Extensions.Logging;
using System;
using System.IO;
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
				try
				{
					if (tcs.Task.IsCompleted)
					{
						logger.LogTrace("Skipping process exit handler as the TaskCompletionSource is already set");
						return;
					}

					try
					{
						var exitCode = handle.ExitCode;

						// Try because this can be invoked twice for weird reasons
						if (tcs.TrySetResult(exitCode))
							logger.LogTrace("Process termination event completed");
						else
							logger.LogTrace("Ignoring duplicate process termination event");
					}
					catch (InvalidOperationException ex)
					{
						if (!tcs.Task.IsCompleted)
							throw;

						logger.LogTrace(ex, "Ignoring expected exception!");
					}
				}
				catch(Exception ex)
				{
					logger.LogError(ex, "Process exit handler exception!");
				}
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

				StringBuilder combinedStringBuilder = null;

				Task<string> outputTask = null;
				Task<string> errorTask = null;
				TaskCompletionSource<object> processStartTcs = null;
				if (readOutput || readError)
				{
					combinedStringBuilder = new StringBuilder();
					processStartTcs = new TaskCompletionSource<object>();

					async Task<string> ConsumeReader(Func<TextReader> readerFunc)
					{
						var stringBuilder = new StringBuilder();
						string text;

						await processStartTcs.Task.ConfigureAwait(false);

						var reader = readerFunc();
						while ((text = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
						{
							combinedStringBuilder.AppendLine();
							combinedStringBuilder.Append(text);
							stringBuilder.AppendLine();
							stringBuilder.Append(text);
						}

						return stringBuilder.ToString();
					}

					if (readOutput)
					{
						outputTask = ConsumeReader(() => handle.StandardOutput);
						handle.StartInfo.RedirectStandardOutput = true;
					}

					if (readError)
					{
						errorTask = ConsumeReader(() => handle.StandardError);
						handle.StartInfo.RedirectStandardError = true;
					}
				}

				var lifetimeTask = AttachExitHandler(handle);

				try
				{
					handle.Start();

					var process = new Process(
						processFeatures,
						handle,
						lifetimeTask,
						outputTask,
						errorTask,
						combinedStringBuilder,
						loggerFactory.CreateLogger<Process>(), false);

					processStartTcs?.SetResult(null);

					return process;
				}
				catch (Exception ex)
				{
					processStartTcs?.SetException(ex);
					throw;
				}
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
