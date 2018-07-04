using System;
using System.Diagnostics;
using System.Threading.Tasks;

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
		/// The backing <see cref="TaskCompletionSource{TResult}"/> for <see cref="Lifetime"/>
		/// </summary>
		readonly TaskCompletionSource<int> lifetimeTask;

		/// <summary>
		/// Construct a <see cref="Session"/>
		/// </summary>
		/// <param name="process">The value of <see cref="process"/></param>
		public Session(Process process)
		{
			this.process = process ?? throw new ArgumentNullException(nameof(process));

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
					PeakMemory = process.PeakWorkingSet64,
					StartupTime = DateTimeOffset.Now - startTime
				};
				if (result.PeakMemory == 0)	//linux, best we can do honestly, test if this even works
					result.PeakMemory = process.WorkingSet64;
				return result;
			}, default, TaskCreationOptions.LongRunning, TaskScheduler.Current);
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
