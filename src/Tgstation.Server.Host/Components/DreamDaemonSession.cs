using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class DreamDaemonSession : IDreamDaemonSession
	{
		/// <inheritdoc />
		public int ProcessId => process.Id;

		/// <inheritdoc />
		public Task SuccessfulStartup { get; }

		/// <inheritdoc />
		public Task<int> Lifetime => lifetimeTask.Task;

		/// <summary>
		/// The actual <see cref="Process"/>
		/// </summary>
		readonly Process process;
		/// <summary>
		/// The backing <see cref="TaskCompletionSource{TResult}"/> for <see cref="Lifetime"/>
		/// </summary>
		readonly TaskCompletionSource<int> lifetimeTask;

		/// <summary>
		/// Construct a <see cref="DreamDaemonSession"/>
		/// </summary>
		/// <param name="process">The value of <see cref="process"/></param>
		public DreamDaemonSession(Process process)
		{
			this.process = process ?? throw new ArgumentNullException(nameof(process));

			SuccessfulStartup = Task.Factory.StartNew(() => process.WaitForInputIdle(), default, TaskCreationOptions.LongRunning, TaskScheduler.Current);
			lifetimeTask = new TaskCompletionSource<int>();
			process.EnableRaisingEvents = true;
			process.Exited += (a, b) => lifetimeTask.SetResult(process.ExitCode);
		}

		/// <inheritdoc />
		public void Dispose() => process.Dispose();

		/// <inheritdoc />
		public void Terminate()
		{
			try
			{
				process.Kill();
				process.WaitForExit();
			}
			catch (InvalidOperationException) { }
		}
	}
}
