using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Common;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Reads from the command pipe opened by the host watchdog.
	/// </summary>
	sealed class CommandPipeReader : BackgroundService
	{
		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="CommandPipeReader"/>.
		/// </summary>
		readonly IServerControl serverControl;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="CommandPipeReader"/>.
		/// </summary>
		readonly ILogger<CommandPipeReader> logger;

		/// <summary>
		/// The <see cref="InternalConfiguration"/> for the <see cref="CommandPipeReader"/>.
		/// </summary>
		readonly InternalConfiguration internalConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="CommandPipeReader"/> class.
		/// </summary>
		/// <param name="serverControl">The value of <see cref="serverControl"/>.</param>
		/// <param name="internalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="internalConfiguration"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public CommandPipeReader(
			IServerControl serverControl,
			IOptions<InternalConfiguration> internalConfigurationOptions,
			ILogger<CommandPipeReader> logger)
		{
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			internalConfiguration = internalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(internalConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		protected override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			logger.LogTrace("Starting...");

			var pipeName = internalConfiguration.CommandPipe;
			if (string.IsNullOrWhiteSpace(pipeName))
			{
				logger.LogDebug("No command pipe name specified in configuration");
				return;
			}

			try
			{
				await using var pipeClient = new AnonymousPipeClientStream(PipeDirection.In, pipeName);
				using var streamReader = new StreamReader(pipeClient, leaveOpen: true);
				while (!cancellationToken.IsCancellationRequested)
				{
					logger.LogTrace("Waiting to read command line...");
					var line = await streamReader.ReadLineAsync().WithToken(cancellationToken);

					logger?.LogInformation("Received pipe command: {command}", line);
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
							logger.LogDebug("Read null from pipe");
							return;
						default:
							logger?.LogWarning("Unrecognized pipe command: {command}", line);
							break;
					}
				}
			}
			catch (OperationCanceledException ex)
			{
				logger?.LogTrace(ex, "Command read task cancelled!");
			}
			catch (Exception ex)
			{
				logger?.LogError(ex, "Command read task errored!");
			}
			finally
			{
				logger?.LogTrace("Command read task exiting...");
			}
		}
	}
}
