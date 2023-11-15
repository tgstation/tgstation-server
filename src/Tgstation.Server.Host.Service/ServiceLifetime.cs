using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Common;
using Tgstation.Server.Host.Watchdog;

namespace Tgstation.Server.Host.Service
{
	/// <summary>
	/// Represents the lifetime of the service.
	/// </summary>
	sealed class ServiceLifetime : ISignalChecker, IAsyncDisposable
	{
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ServerService"/>.
		/// </summary>
		readonly ILogger<ServiceLifetime> logger;

		/// <summary>
		/// The <see cref="Task"/> that represents the running <see cref="ServerService"/>.
		/// </summary>
		readonly Task watchdogTask;

		/// <summary>
		/// The <see cref="cancellationTokenSource"/> for the <see cref="ServerService"/>.
		/// </summary>
		readonly CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// The <see cref="AnonymousPipeServerStream"/> for sending <see cref="PipeCommands"/> to the server process.
		/// </summary>
		AnonymousPipeServerStream? commandPipeServer;

		/// <summary>
		/// The <see cref="AnonymousPipeServerStream"/> for receiving the <see cref="PipeCommands.CommandStartupComplete"/>.
		/// </summary>
		AnonymousPipeServerStream? readyPipeServer;

		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceLifetime"/> class.
		/// </summary>
		/// <param name="stopService">An <see cref="Action"/> to manually stop the service.</param>
		/// <param name="watchdogFactory">A <see cref="Func{T, TResult}"/> taking a <see cref="ISignalChecker"/> and returning the <see cref="IWatchdog"/> to run.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="args">The arguments for the <see cref="IWatchdog"/>.</param>
		public ServiceLifetime(Action stopService, Func<ISignalChecker, IWatchdog> watchdogFactory, ILogger<ServiceLifetime> logger, string[] args)
		{
			ArgumentNullException.ThrowIfNull(stopService);
			ArgumentNullException.ThrowIfNull(watchdogFactory);
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			ArgumentNullException.ThrowIfNull(args);

			cancellationTokenSource = new CancellationTokenSource();
			watchdogTask = RunWatchdog(
				stopService,
				watchdogFactory(this),
				args,
				cancellationTokenSource.Token);
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			cancellationTokenSource.Cancel();
			await watchdogTask;
			cancellationTokenSource.Dispose();

			if (commandPipeServer != null)
				await commandPipeServer.DisposeAsync();

			if (readyPipeServer != null)
				await readyPipeServer.DisposeAsync();
		}

		/// <inheritdoc />
		public async ValueTask CheckSignals(Func<string, (int, Task)> startChildAndGetPid, CancellationToken cancellationToken)
		{
			try
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
			finally
			{
				readyPipeServer = null;
				commandPipeServer = null;
			}
		}

		/// <summary>
		/// Handle a custom service <paramref name="command"/>.
		/// </summary>
		/// <param name="command">The <see cref="int"/> command sent to the service.</param>
		public void HandleCustomCommand(int command)
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

		/// <summary>
		/// Executes the <paramref name="watchdog"/>, stopping the service if it exits.
		/// </summary>
		/// <param name="stopService">An <see cref="Action"/> to manually stop the service.</param>
		/// <param name="watchdog">The <see cref="IWatchdog"/> to run.</param>
		/// <param name="args">The arguments for the <paramref name="watchdog"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task RunWatchdog(Action stopService, IWatchdog watchdog, string[] args, CancellationToken cancellationToken)
		{
			var localWatchdogTask = watchdog.RunAsync(false, args, cancellationToken);

			if (!localWatchdogTask.IsCompleted && (await watchdog.InitialHostVersion) >= new Version(5, 14, 0))
				if (readyPipeServer != null)
				{
					logger.LogInformation("Waiting for host to finish starting...");
					using var streamReader = new StreamReader(
						readyPipeServer,
						Encoding.UTF8,
						leaveOpen: true);

					var line = streamReader.ReadLine(); // Intentionally blocking service startup
					logger.LogDebug("Pipe read: {line}", line);

					// Maybe we'll use this pipe more in the future, but for now leaving it open is just a resource waste
					readyPipeServer.Dispose();
				}
				else
					logger.LogError("Watchdog started and ready pipe was not initialized!");

			await localWatchdogTask;

			try
			{
				stopService();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error stopping service!");
			}
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
