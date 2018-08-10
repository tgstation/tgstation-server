using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
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
		/// Create a <see cref="Task{TResult}"/> resulting in the exit code of a given <paramref name="handle"/>
		/// </summary>
		/// <param name="handle">The <see cref="System.Diagnostics.Process"/> to attach the <see cref="Task{TResult}"/> for</param>
		/// <returns>A new <see cref="Task{TResult}"/> resulting in the exit code of <paramref name="handle"/></returns>
		static Task<int> AttachExitHandler(System.Diagnostics.Process handle)
		{
			handle.EnableRaisingEvents = true;
			var tcs = new TaskCompletionSource<int>();
			handle.Exited += (a, b) => tcs.SetResult(handle.ExitCode);
			return tcs.Task;
		}

		/// <summary>
		/// Construct a <see cref="ProcessExecutor"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public ProcessExecutor(ILogger<ProcessExecutor> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public IProcess GetProcess(int id)
		{
			logger.LogDebug("Attaching to process {0}...", id);
			var handle = System.Diagnostics.Process.GetProcessById(id);
			try
			{
				return new Process(handle, AttachExitHandler(handle), null, null, null);
			}
			catch
			{
				handle.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public IProcess LaunchProcess(string fileName, string workingDirectory, string arguments, bool readOutput, bool readError)
		{
			logger.LogDebug("Launching process in {0}: {1} {2}", workingDirectory, fileName, arguments);
			var handle = new System.Diagnostics.Process();
			try
			{
				handle.StartInfo.FileName = fileName;
				handle.StartInfo.Arguments = arguments;
				handle.StartInfo.WorkingDirectory = workingDirectory;
				
				StringBuilder outputStringBuilder = null, errorStringBuilder = null, combinedStringBuilder = null;
				if (readOutput || readError)
				{
					handle.StartInfo.UseShellExecute = false;
					combinedStringBuilder = new StringBuilder();
					if (readOutput)
					{
						outputStringBuilder = new StringBuilder();
						handle.StartInfo.RedirectStandardOutput = true;
						var eventHandler = new DataReceivedEventHandler(
							delegate (object sender, DataReceivedEventArgs e)
							{
								combinedStringBuilder.Append(Environment.NewLine);
								combinedStringBuilder.Append(e.Data);
								outputStringBuilder.Append(Environment.NewLine);
								outputStringBuilder.Append(e.Data);
							}
						);
						handle.OutputDataReceived += eventHandler;
					}
					if (readError)
					{
						errorStringBuilder = new StringBuilder();
						handle.StartInfo.RedirectStandardError = true;
						var eventHandler = new DataReceivedEventHandler(
							delegate (object sender, DataReceivedEventArgs e)
							{
								combinedStringBuilder.Append(Environment.NewLine);
								combinedStringBuilder.Append(e.Data);
								errorStringBuilder.Append(Environment.NewLine);
								errorStringBuilder.Append(e.Data);
							}
						);
						handle.ErrorDataReceived += eventHandler;
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

				return new Process(handle, lifetimeTask, outputStringBuilder, errorStringBuilder, combinedStringBuilder);
			}
			catch
			{
				handle.Dispose();
				throw;
			}
		}
	}
}