using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	sealed class ProcessExecutor : IProcessExecutor
	{
		/// <summary>
		/// <see cref="ReaderWriterLockSlim"/> for <see cref="WithProcessLaunchExclusivity(Action)"/>.
		/// </summary>
		static readonly ReaderWriterLockSlim ExclusiveProcessLaunchLock = new();

		/// <summary>
		/// The <see cref="IProcessFeatures"/> for the <see cref="ProcessExecutor"/>.
		/// </summary>
		readonly IProcessFeatures processFeatures;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="ProcessExecutor"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ProcessExecutor"/>.
		/// </summary>
		readonly ILogger<ProcessExecutor> logger;

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="ProcessExecutor"/>.
		/// </summary>
		readonly ILoggerFactory loggerFactory;

		/// <summary>
		/// Runs a given <paramref name="action"/> making sure to not launch any processes while its running.
		/// </summary>
		/// <param name="action">The <see cref="Action"/> to execute.</param>
		public static void WithProcessLaunchExclusivity(Action action)
		{
			ExclusiveProcessLaunchLock.EnterWriteLock();
			try
			{
				action();
			}
			finally
			{
				ExclusiveProcessLaunchLock.ExitWriteLock();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ProcessExecutor"/> class.
		/// </summary>
		/// <param name="processFeatures">The value of <see cref="processFeatures"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/>.</param>
		public ProcessExecutor(
			IProcessFeatures processFeatures,
			IIOManager ioManager,
			ILogger<ProcessExecutor> logger,
			ILoggerFactory loggerFactory)
		{
			this.processFeatures = processFeatures ?? throw new ArgumentNullException(nameof(processFeatures));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
		}

		/// <inheritdoc />
		public IProcess? GetProcess(int id)
		{
			logger.LogDebug("Attaching to process {pid}...", id);
			global::System.Diagnostics.Process handle;
			try
			{
				handle = global::System.Diagnostics.Process.GetProcessById(id);
			}
			catch (Exception e)
			{
				logger.LogDebug(e, "Unable to get process {pid}!", id);
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
		public async ValueTask<IProcess> LaunchProcess(
			string fileName,
			string workingDirectory,
			string arguments,
			CancellationToken cancellationToken,
			IReadOnlyDictionary<string, string>? environment,
			string? fileRedirect,
			bool readStandardHandles,
			bool noShellExecute)
		{
			ArgumentNullException.ThrowIfNull(fileName);
			ArgumentNullException.ThrowIfNull(workingDirectory);
			ArgumentNullException.ThrowIfNull(arguments);

			var enviromentLogLines = environment == null
				? String.Empty
				: String.Concat(environment.Select(kvp => $"{Environment.NewLine}\t- {kvp.Key}={kvp.Value}"));
			if (noShellExecute)
				logger.LogDebug(
				"Launching process in {workingDirectory}: {exe} {arguments}{environment}",
				workingDirectory,
				fileName,
				arguments,
				enviromentLogLines);
			else
				logger.LogDebug(
				"Shell launching process in {workingDirectory}: {exe} {arguments}{environment}",
				workingDirectory,
				fileName,
				arguments,
				enviromentLogLines);

			var handle = new global::System.Diagnostics.Process();
			try
			{
				handle.StartInfo.FileName = fileName;
				handle.StartInfo.Arguments = arguments;
				if (environment != null)
					foreach (var kvp in environment)
						handle.StartInfo.Environment.Add(kvp!);

				handle.StartInfo.WorkingDirectory = workingDirectory;

				handle.StartInfo.UseShellExecute = !noShellExecute;

				Task<string?>? readTask = null;
				CancellationTokenSource? disposeCts = null;
				try
				{
					TaskCompletionSource<int>? processStartTcs = null;
					if (readStandardHandles)
					{
						processStartTcs = new TaskCompletionSource<int>();
						disposeCts = new CancellationTokenSource();
						readTask = ConsumeReaders(handle, processStartTcs.Task, fileRedirect, disposeCts.Token);
					}

					int pid;
					try
					{
						ExclusiveProcessLaunchLock.EnterReadLock();
						try
						{
							handle.Start();
						}
						finally
						{
							ExclusiveProcessLaunchLock.ExitReadLock();
						}

						try
						{
							pid = await processFeatures.HandleProcessStart(handle, cancellationToken);
						}
						catch
						{
							handle.Kill();
							throw;
						}

						processStartTcs?.SetResult(pid);
					}
					catch (Exception ex)
					{
						processStartTcs?.SetException(ex);
						throw;
					}

					var process = new Process(
						processFeatures,
						handle,
						disposeCts,
						readTask,
						loggerFactory.CreateLogger<Process>(),
						false);

					return process;
				}
				catch
				{
					disposeCts?.Dispose();
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
		public IProcess? GetProcessByName(string name)
		{
			logger.LogTrace("GetProcessByName: {processName}...", name ?? throw new ArgumentNullException(nameof(name)));
			var procs = global::System.Diagnostics.Process.GetProcessesByName(name);
			global::System.Diagnostics.Process? handle = null;
			foreach (var proc in procs)
				if (handle == null)
					handle = proc;
				else
				{
					logger.LogTrace("Disposing extra found PID: {pid}...", proc.Id);
					proc.Dispose();
				}

			if (handle == null)
				return null;

			return CreateFromExistingHandle(handle);
		}

		/// <summary>
		/// Consume the stdout/stderr streams into a <see cref="Task"/>.
		/// </summary>
		/// <param name="handle">The <see cref="global::System.Diagnostics.Process"/>.</param>
		/// <param name="startupAndPid">The <see cref="Task{TResult}"/> resulting in the <see cref="global::System.Diagnostics.Process.Id"/> of the started process.</param>
		/// <param name="fileRedirect">The optional path to redirect the streams to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the program's output/error text if <paramref name="fileRedirect"/> is <see langword="null"/>, <see langword="null"/> otherwise.</returns>
		async Task<string?> ConsumeReaders(global::System.Diagnostics.Process handle, Task<int> startupAndPid, string? fileRedirect, CancellationToken cancellationToken)
		{
			handle.StartInfo.RedirectStandardOutput = true;
			handle.StartInfo.RedirectStandardError = true;

			bool writingToFile;
			await using var fileStream = (writingToFile = fileRedirect != null) ? ioManager.CreateAsyncSequentialWriteStream(fileRedirect!) : null;
			await using var fileWriter = fileStream != null ? new StreamWriter(fileStream) : null;

			var stringBuilder = fileStream == null ? new StringBuilder() : null;

			var dataChannel = Channel.CreateUnbounded<string>(
				new UnboundedChannelOptions
				{
					AllowSynchronousContinuations = !writingToFile,
					SingleReader = true,
					SingleWriter = false,
				});

			var handlesOpen = 2;
			async void DataReceivedHandler(object sender, DataReceivedEventArgs eventArgs)
			{
				var line = eventArgs.Data;
				if (line == null)
				{
					var handlesRemaining = Interlocked.Decrement(ref handlesOpen);
					if (handlesRemaining == 0)
						dataChannel.Writer.Complete();

					return;
				}

				try
				{
					await dataChannel.Writer.WriteAsync(line, cancellationToken);
				}
				catch (OperationCanceledException ex)
				{
					logger.LogWarning(ex, "Handle channel write interrupted!");
				}
			}

			handle.OutputDataReceived += DataReceivedHandler;
			handle.ErrorDataReceived += DataReceivedHandler;

			async ValueTask OutputWriter()
			{
				var enumerable = dataChannel.Reader.ReadAllAsync(cancellationToken);
				if (writingToFile)
				{
					var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
					var nextEnumeration = enumerator.MoveNextAsync();
					while (await nextEnumeration)
					{
						var text = enumerator.Current;
						nextEnumeration = enumerator.MoveNextAsync();
						await fileWriter!.WriteLineAsync(text.AsMemory(), cancellationToken);

						if (!nextEnumeration.IsCompleted)
							await fileWriter.FlushAsync(cancellationToken);
					}
				}
				else
					await foreach (var text in enumerable)
						stringBuilder!.AppendLine(text);
			}

			var pid = await startupAndPid;
			logger.LogTrace("Starting read for PID {pid}...", pid);

			using (cancellationToken.Register(() => dataChannel.Writer.TryComplete()))
			{
				handle.BeginOutputReadLine();
				using (cancellationToken.Register(handle.CancelOutputRead))
				{
					handle.BeginErrorReadLine();
					using (cancellationToken.Register(handle.CancelErrorRead))
					{
						try
						{
							await OutputWriter();

							logger.LogTrace("Finished read for PID {pid}", pid);
						}
						catch (OperationCanceledException ex)
						{
							logger.LogWarning(ex, "PID {pid} stream reading interrupted!", pid);
							if (writingToFile)
								await fileWriter!.WriteLineAsync("-- Process detached, log truncated. This is likely due a to TGS restart --");
						}
					}
				}
			}

			return stringBuilder?.ToString();
		}

		/// <summary>
		/// Create a <see cref="IProcess"/> given an existing <paramref name="handle"/>.
		/// </summary>
		/// <param name="handle">The <see cref="global::System.Diagnostics.Process"/> to create a <see cref="IProcess"/> from.</param>
		/// <returns>The <see cref="IProcess"/> based on <paramref name="handle"/>.</returns>
		Process CreateFromExistingHandle(global::System.Diagnostics.Process handle)
		{
			try
			{
				var pid = handle.Id;
				return new Process(
					processFeatures,
					handle,
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
