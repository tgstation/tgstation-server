using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Tgstation.Server.Tests.Live
{
	sealed class HardFailLoggerProvider : ILoggerProvider
	{
		public static bool BlockFails { get; set; }

		public static Task FailureSource => failureSink.Task;

		static readonly TaskCompletionSource failureSink = new (TaskCreationOptions.RunContinuationsAsynchronously);

		public ILogger CreateLogger(string categoryName) => new HardFailLogger(ex =>
		{
			if (!BlockFails)
				failureSink.TrySetException(ex);
		});

		public void Dispose() { }
	}
}
