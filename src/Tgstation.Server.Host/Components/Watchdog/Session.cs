using System;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <inheritdoc />
	sealed class Session : ISession
	{
		/// <inheritdoc />
		public int Id => process.Id;

		/// <inheritdoc />
		public Task Startup => process.Startup;

		/// <inheritdoc />
		public Task<LaunchResult> LaunchResult { get; }

		/// <inheritdoc />
		public Task<int> Lifetime => process.Lifetime;

		/// <summary>
		/// The actual <see cref="IProcess"/>
		/// </summary>
		readonly IProcess process;
		/// <summary>
		/// The <see cref="IByondExecutableLock"/> for the <see cref="process"/>
		/// </summary>
		readonly IByondExecutableLock byondLock;

		/// <summary>
		/// Construct a <see cref="Session"/>
		/// </summary>
		/// <param name="process">The value of <see cref="process"/></param>
		/// <param name="byondLock">The value of <see cref="byondLock"/></param>
		public Session(IProcess process, IByondExecutableLock byondLock)
		{
			this.process = process ?? throw new ArgumentNullException(nameof(process));
			this.byondLock = byondLock ?? throw new ArgumentNullException(nameof(byondLock));

			async Task<LaunchResult> GetLaunchResult()
			{
				var startTime = DateTimeOffset.Now;
				await process.Startup.ConfigureAwait(false);
				var result = new LaunchResult
				{
					ExitCode = process.Lifetime.IsCompleted ? (int?)await process.Lifetime.ConfigureAwait(false) : null,
					StartupTime = DateTimeOffset.Now - startTime
				};
				return result;
			};
			LaunchResult = GetLaunchResult();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			process.Dispose();
			byondLock.Dispose();
		}

		/// <inheritdoc />
		public void Terminate() => process.Terminate();

		/// <inheritdoc />
		public string GetErrorOutput() => process.GetErrorOutput();

		/// <inheritdoc />
		public string GetStandardOutput() => process.GetStandardOutput();

		/// <inheritdoc />
		public string GetCombinedOutput() => process.GetCombinedOutput();
	}
}
