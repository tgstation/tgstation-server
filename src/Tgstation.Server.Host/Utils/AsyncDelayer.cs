using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Tgstation.Server.Host.Utils
{
	/// <inheritdoc />
	sealed class AsyncDelayer : IAsyncDelayer
	{
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="AsyncDelayer"/>.
		/// </summary>
		readonly ILogger<AsyncDelayer> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="AsyncDelayer"/> class.
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public AsyncDelayer(ILogger<AsyncDelayer> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public async ValueTask Delay(TimeSpan timeSpan, CancellationToken cancellationToken)
		{
			// https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.delay?view=net-8.0#system-threading-tasks-task-delay(system-timespan)
			const uint DelayMinutesLimit = UInt32.MaxValue - 1;
			Debug.Assert(DelayMinutesLimit == 4294967294, "Delay limit assertion failure!");

			var maxDelayIterations = 0UL;
			if (timeSpan.TotalMilliseconds >= UInt32.MaxValue)
			{
				maxDelayIterations = (ulong)Math.Floor(timeSpan.TotalMilliseconds / DelayMinutesLimit);
				logger.LogDebug("Breaking interval into {iterationCount} iterations", maxDelayIterations + 1);
				timeSpan = TimeSpan.FromMilliseconds(timeSpan.TotalMilliseconds - (maxDelayIterations * DelayMinutesLimit));
			}

			if (maxDelayIterations > 0)
			{
				var longDelayTimeSpan = TimeSpan.FromMilliseconds(DelayMinutesLimit);
				for (var i = 0UL; i < maxDelayIterations; ++i)
				{
					logger.LogTrace("Long delay #{iteration}...", i + 1);
					await Task.Delay(longDelayTimeSpan, cancellationToken);
				}

				logger.LogTrace("Final delay iteration #{iteration}...", maxDelayIterations + 1);
			}

			await Task.Delay(timeSpan, cancellationToken);
		}
	}
}
