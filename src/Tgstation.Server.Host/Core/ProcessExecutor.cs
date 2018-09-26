using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class ProcessExecutor : IProcessExecutor
	{
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
		/// <param name="handle">The <see cref="System.Diagnostics.Process"/> to attach the <see cref="Task{TResult}"/> for</param>
		/// <returns>A new <see cref="Task{TResult}"/> resulting in the exit code of <paramref name="handle"/></returns>
		static Task<int> AttachExitHandler(System.Diagnostics.Process handle)
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
				tcs.SetResult(exitCode);
			};
			return tcs.Task;
		}

		/// <summary>
		/// Construct a <see cref="ProcessExecutor"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/></param>
		public ProcessExecutor(ILogger<ProcessExecutor> logger, ILoggerFactory loggerFactory)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
		}

		/// <inheritdoc />
		public IProcess GetProcess(int id)
		{
			logger.LogDebug("Attaching to process {0}...", id);
			System.Diagnostics.Process handle;
			try
			{
				handle = System.Diagnostics.Process.GetProcessById(id);
			}
			catch(Exception e)
			{
				logger.LogDebug("Unable to get process {0}! Exception: {1}", id, e);
				return null;
			}
			try
			{
				return new Process(handle, AttachExitHandler(handle), null, null, null, loggerFactory.CreateLogger<Process>(), true);
			}
			catch
			{
				handle.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public IProcess LaunchProcess(string fileName, string workingDirectory, string arguments, bool readOutput, bool readError, bool noShellExecute)
		{
			logger.LogDebug("Launching process in {0}: {1} {2}", workingDirectory, fileName, arguments);
			var handle = new System.Diagnostics.Process();
			try
			{
				handle.StartInfo.FileName = fileName;
				handle.StartInfo.Arguments = arguments;
				handle.StartInfo.WorkingDirectory = workingDirectory;

				handle.StartInfo.UseShellExecute = !(noShellExecute || readOutput || readError);

				StringBuilder outputStringBuilder = null, errorStringBuilder = null, combinedStringBuilder = null;
				if (readOutput || readError)
				{
					combinedStringBuilder = new StringBuilder();
					if (readOutput)
					{
						outputStringBuilder = new StringBuilder();
						handle.StartInfo.RedirectStandardOutput = true;
						handle.OutputDataReceived += (sender, e) =>
						{
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
						handle.ErrorDataReceived += (sender, e) =>
						{
							combinedStringBuilder.Append(Environment.NewLine);
							combinedStringBuilder.Append(e.Data);
							errorStringBuilder.Append(Environment.NewLine);
							errorStringBuilder.Append(e.Data);
						};
					}
				}

				var lifetimeTask = AttachExitHandler(handle);

				handle.Start();
				try
				{
					if (readOutput)
						handle.BeginOutputReadLine();
				}
				catch (InvalidOperationException) { }
				try
				{
					if (readError)
						handle.BeginErrorReadLine();
				}
				catch (InvalidOperationException) { }

				return new Process(handle, lifetimeTask, outputStringBuilder, errorStringBuilder, combinedStringBuilder, loggerFactory.CreateLogger<Process>(), false);
			}
			catch
			{
				handle.Dispose();
				throw;
			}
		}
	}
}
