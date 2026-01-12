using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Common;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Reads from the command pipe opened by the host watchdog.
	/// </summary>
	sealed class CommandPipeManager : BackgroundService
	{
		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="CommandPipeManager"/>.
		/// </summary>
		readonly IServerControl serverControl;

		/// <summary>
		/// The <see cref="IInstanceManager"/> for the <see cref="CommandPipeManager"/>.
		/// </summary>
		readonly IInstanceManager instanceManager;

		/// <summary>
		/// The <see cref="IOptions{TOptions}"/> of <see cref="InternalConfiguration"/> for the <see cref="CommandPipeManager"/>.
		/// </summary>
		readonly IOptions<InternalConfiguration> internalConfigurationOptions;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="CommandPipeManager"/>.
		/// </summary>
		readonly ILogger<CommandPipeManager> logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandPipeManager"/> class.
		/// </summary>
		/// <param name="serverControl">The value of <see cref="serverControl"/>.</param>
		/// <param name="instanceManager">The value of <see cref="instanceManager"/>.</param>
		/// <param name="internalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="internalConfigurationOptions"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public CommandPipeManager(
			IServerControl serverControl,
			IInstanceManager instanceManager,
			IOptions<InternalConfiguration> internalConfigurationOptions,
			ILogger<CommandPipeManager> logger)
		{
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.internalConfigurationOptions = internalConfigurationOptions ?? throw new ArgumentNullException(nameof(internalConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		protected override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			logger.LogTrace("Starting...");

			// grab both pipes asap so we can close them on error
			var commandPipe = internalConfigurationOptions.Value.CommandPipe;
			var supportsPipeCommands = !String.IsNullOrWhiteSpace(commandPipe);
			await using var commandPipeClient = supportsPipeCommands
				? new AnonymousPipeClientStream(
					PipeDirection.In,
					commandPipe!)
				: null;

			if (!supportsPipeCommands)
				logger.LogDebug("No command pipe name specified in configuration");

			var readyPipe = internalConfigurationOptions.Value.ReadyPipe;
			var supportsReadyNotification = !String.IsNullOrWhiteSpace(readyPipe);
			if (supportsReadyNotification)
			{
				await using var readyPipeClient = new AnonymousPipeClientStream(
					PipeDirection.Out,
					readyPipe!);

				logger.LogTrace("Waiting to send ready notification...");
				await instanceManager.Ready.WaitAsync(cancellationToken);

				using var streamWriter = new StreamWriter(readyPipeClient, Encoding.UTF8, leaveOpen: true);
				await streamWriter.WriteLineAsync(PipeCommands.CommandStartupComplete.AsMemory(), cancellationToken);
			}
			else
				logger.LogDebug("No ready pipe name specified in configuration");

			if (!supportsPipeCommands)
				return;

			try
			{
				using var streamReader = new StreamReader(commandPipeClient!, Encoding.UTF8, leaveOpen: true);
				while (!cancellationToken.IsCancellationRequested)
				{
					logger.LogTrace("Waiting to read command line...");
					var line = await streamReader.ReadLineAsync(cancellationToken);

					logger.LogInformation("Received pipe command: {command}", line);
					switch (line)
					{
						case PipeCommands.CommandStop:
							await serverControl.Die(null);
							break;
						case PipeCommands.CommandGracefulShutdown:
							await serverControl.GracefulShutdown(false);
							break;
						case PipeCommands.CommandDetachingShutdown:
							await serverControl.GracefulShutdown(true);
							break;
						case null:
							logger.LogError("Read null from pipe!");
							return;
						default:
							logger.LogWarning("Unrecognized pipe command: {command}", line);
							break;
					}
				}
			}
			catch (OperationCanceledException ex)
			{
				logger.LogTrace(ex, "Command read task cancelled!");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Command read task errored!");
			}
			finally
			{
				logger.LogTrace("Command read task exiting...");
			}
		}
	}
}
