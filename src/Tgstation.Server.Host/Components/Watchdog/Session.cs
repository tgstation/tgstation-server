using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Byond;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class Session : ISession
	{
		/// <inheritdoc />
		public int ProcessId => process.Id;

		/// <inheritdoc />
		public Task<LaunchResult> LaunchResult { get; }

		/// <inheritdoc />
		public Task<int> Lifetime => lifetimeTask.Task;

		/// <summary>
		/// The actual <see cref="Process"/>
		/// </summary>
		readonly Process process;
		/// <summary>
		/// The <see cref="IByondExecutableLock"/> for the <see cref="process"/>
		/// </summary>
		readonly IByondExecutableLock byondLock;

		/// <summary>
		/// The backing <see cref="TaskCompletionSource{TResult}"/> for <see cref="Lifetime"/>
		/// </summary>
		readonly TaskCompletionSource<int> lifetimeTask;

		/// <summary>
		/// Construct a <see cref="Session"/>
		/// </summary>
		/// <param name="process">The value of <see cref="process"/></param>
		/// <param name="byondLock">The value of <see cref="byondLock"/></param>
		public Session(Process process, IByondExecutableLock byondLock)
		{
			this.process = process ?? throw new ArgumentNullException(nameof(process));
			this.byondLock = byondLock ?? throw new ArgumentNullException(nameof(byondLock));

			LaunchResult = Task.Factory.StartNew(() =>
			{
				var startTime = DateTimeOffset.Now;
				try
				{
					process.WaitForInputIdle();
				}
				catch (InvalidOperationException) { }
				var result = new LaunchResult
				{
					ExitCode = process.HasExited ? (int?)process.ExitCode : null,
					StartupTime = DateTimeOffset.Now - startTime
				};
				return result;
			}, default, TaskCreationOptions.LongRunning, TaskScheduler.Current);
			lifetimeTask = new TaskCompletionSource<int>();
			try
			{
				process.EnableRaisingEvents = true;
				process.Exited += (a, b) => lifetimeTask.TrySetResult(process.ExitCode);
			}
			catch (InvalidOperationException)
			{
				//dead proccess
				lifetimeTask.TrySetResult(process.ExitCode);
			}
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
