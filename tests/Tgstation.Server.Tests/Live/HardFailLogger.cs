using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tgstation.Server.Tests.Live
{
	sealed class HardFailLogger : ILogger
	{
		readonly Action<Exception> failureSink;

		public HardFailLogger(Action<Exception> failureSink)
		{
			this.failureSink = failureSink ?? throw new ArgumentNullException(nameof(failureSink));
		}

		public IDisposable BeginScope<TState>(TState state) => Task.CompletedTask;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			var logMessage = formatter(state, exception);
			if ((logLevel == LogLevel.Error
				&& !((exception is BadHttpRequestException) && logMessage.Contains("Unexpected end of request content.")) // canceled request
				&& !logMessage.StartsWith("Error disconnecting connection ")
				&& !(logMessage.StartsWith("An exception occurred while iterating over the results of a query for context type") && (exception is OperationCanceledException || exception?.InnerException is OperationCanceledException))
				&& !(logMessage.StartsWith("An error occurred using the connection to database ") && (exception is OperationCanceledException || exception?.InnerException is OperationCanceledException))
				&& !(logMessage.StartsWith("An exception occurred in the database while saving changes for context type") && (exception is OperationCanceledException || exception?.InnerException is OperationCanceledException)))
				|| (logLevel == LogLevel.Critical && logMessage != "DropDatabase configuration option set! Dropping any existing database..."))
			{
				failureSink(new AssertFailedException($"TGS ERR: {logMessage}"));
			}
		}
	}
}
