using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Versioning;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

using Tgstation.Server.Common;
using Tgstation.Server.Host.Common;
using Tgstation.Server.Host.Watchdog;

namespace Tgstation.Server.Host.Service
{
	/// <summary>
	/// Represents a <see cref="IWatchdog"/> as a <see cref="ServiceBase"/>.
	/// </summary>
	[SupportedOSPlatform("windows")]
	sealed class ServerService : ServiceBase, ISignalChecker
	{
		/// <summary>
		/// The canonical windows service name.
		/// </summary>
		public const string Name = Constants.CanonicalPackageName;

		/// <summary>
		/// The <see cref="IWatchdog"/> for the <see cref="ServerService"/>.
		/// </summary>
		readonly IWatchdogFactory watchdogFactory;

		/// <summary>
		/// The <see cref="Array"/> of command line arguments the service was invoked with.
		/// </summary>
		readonly string[] commandLineArguments;

		/// <summary>
		/// The minimum <see cref="LogLevel"/> for the <see cref="EventLog"/>.
		/// </summary>
		readonly LogLevel minimumLogLevel;

		/// <summary>
		/// The <see cref="ILoggerFactory"/> used by the <see cref="ServerService"/>.
		/// </summary>
		ILoggerFactory loggerFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ServerService"/>.
		/// </summary>
		ILogger<ServerService> logger;

		/// <summary>
		/// The <see cref="Task"/> that represents the running <see cref="ServerService"/>.
		/// </summary>
		Task watchdogTask;

		/// <summary>
		/// The <see cref="cancellationTokenSource"/> for the <see cref="ServerService"/>.
		/// </summary>
		CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// The <see cref="AnonymousPipeServerStream"/> for sending <see cref="PipeCommands"/> to the server process.
		/// </summary>
		AnonymousPipeServerStream commandPipeServer;

		/// <summary>
		/// The <see cref="AnonymousPipeServerStream"/> for receiving the <see cref="PipeCommands.CommandStartupComplete"/>.
		/// </summary>
		AnonymousPipeServerStream readyPipeServer;

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerService"/> class.
		/// </summary>
		/// <param name="watchdogFactory">The value of <see cref="watchdogFactory"/>.</param>
		/// <param name="commandLineArguments">The value of <see cref="commandLineArguments"/>.</param>
		/// <param name="minimumLogLevel">The minimum <see cref="LogLevel"/> to record in the event log.</param>
		public ServerService(IWatchdogFactory watchdogFactory, string[] commandLineArguments, LogLevel minimumLogLevel)
		{
			this.watchdogFactory = watchdogFactory ?? throw new ArgumentNullException(nameof(watchdogFactory));
			this.commandLineArguments = commandLineArguments ?? throw new ArgumentNullException(nameof(commandLineArguments));
			this.minimumLogLevel = minimumLogLevel;
			ServiceName = Name;
		}

		/// <inheritdoc />
		public async Task CheckSignals(Func<string, (int, Task)> startChildAndGetPid, CancellationToken cancellationToken)
		{
			await using (commandPipeServer = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable))
			await using (readyPipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
			{
				var (_, lifetimeTask) = startChildAndGetPid($"--Internal:CommandPipe={commandPipeServer.GetClientHandleAsString()} --Internal:ReadyPipe={readyPipeServer.GetClientHandleAsString()}");
				commandPipeServer.DisposeLocalCopyOfClientHandle();
				readyPipeServer.DisposeLocalCopyOfClientHandle();
				await lifetimeTask;
			}
		}

		/// <summary>
		/// Executes the <see cref="ServerService"/>.
		/// </summary>
		public void Run() => Run(this);

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				loggerFactory?.Dispose();
				cancellationTokenSource?.Dispose();
				commandPipeServer?.Dispose();
				readyPipeServer?.Dispose();
			}

			base.Dispose(disposing);
		}

		/// <inheritdoc />
		protected override void OnCustomCommand(int command)
		{
			var commandsToCheck = PipeCommands.AllCommands;
			foreach (var stringCommand in commandsToCheck)
			{
				var commandId = PipeCommands.GetServiceCommandId(stringCommand);
				if (command == commandId)
				{
					SendCommandToHostThroughPipe(stringCommand);
					return;
				}
			}

			logger.LogWarning("Received unknown service command: {command}", command);
		}

		/// <inheritdoc />
		protected override void OnStart(string[] args)
		{
			if (loggerFactory == null)
			{
				loggerFactory = LoggerFactory.Create(builder => builder.AddEventLog(new EventLogSettings
				{
					LogName = EventLog.Log,
					MachineName = EventLog.MachineName,
					SourceName = EventLog.Source,
					Filter = (message, logLevel) => logLevel >= minimumLogLevel,
				}));

				logger = loggerFactory.CreateLogger<ServerService>();
			}

			var watchdog = watchdogFactory.CreateWatchdog(this, loggerFactory);

			cancellationTokenSource?.Dispose();
			cancellationTokenSource = new CancellationTokenSource();

			var newArgs = new List<string>(commandLineArguments.Length + args.Length + 1)
			{
				"--General:SetupWizardMode=Never",
			};

			newArgs.AddRange(commandLineArguments);
			newArgs.AddRange(args);

			watchdogTask = RunWatchdog(watchdog, newArgs.ToArray(), cancellationTokenSource.Token);

			if (!watchdogTask.IsCompleted && watchdog.InitialHostVersion >= new Version(5, 14, 0))
			{
				logger.LogInformation("Waiting for host to finish starting...");
				using var streamReader = new StreamReader(
					readyPipeServer,
					Encoding.UTF8,
					leaveOpen: true);

				var line = streamReader.ReadLine(); // Intentionally blocking service startup
				logger.LogDebug("Pipe read: {line}", line);
			}

			// Maybe we'll use this pipe more in the future, but for now leaving it open is just a resource waste
			readyPipeServer.Dispose();
		}

		/// <inheritdoc />
		protected override void OnStop()
		{
			cancellationTokenSource.Cancel();
			watchdogTask.GetAwaiter().GetResult();
		}

		/// <summary>
		/// Executes the <paramref name="watchdog"/>, stopping the service if it exits.
		/// </summary>
		/// <param name="watchdog">The <see cref="IWatchdog"/> to run.</param>
		/// <param name="args">The arguments for the <paramref name="watchdog"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task RunWatchdog(IWatchdog watchdog, string[] args, CancellationToken cancellationToken)
		{
			await watchdog.RunAsync(false, args, cancellationToken);

			async void StopServiceAsync()
			{
				try
				{
					await Task.Run(Stop, cancellationToken); // DCT intentional
				}
				catch (OperationCanceledException ex)
				{
					logger.LogTrace(ex, "Stopping service cancelled!");
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error stopping service!");
				}
			}

			StopServiceAsync();
		}

		/// <summary>
		/// Sends a command to the main server process.
		/// </summary>
		/// <param name="command">One of the <see cref="PipeCommands"/>.</param>
		void SendCommandToHostThroughPipe(string command)
		{
			var localPipeServer = commandPipeServer;
			if (localPipeServer == null)
			{
				logger.LogWarning("Unable to send command \"{command}\" to main server process. Is the service running?", command);
				return;
			}

			logger.LogDebug("Send command: {command}", command);
			try
			{
				var encoding = Encoding.UTF8;
				using var streamWriter = new StreamWriter(
					localPipeServer,
					encoding,
					PipeCommands
						.AllCommands
						.Select(
							command => encoding.GetByteCount(
								command + Environment.NewLine))
						.Max(),
					true);
				streamWriter.WriteLine(command);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error attempting to send command \"{command}\"", command);
			}
		}
	}
}
