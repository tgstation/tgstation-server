using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Chat.Commands;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <inheritdoc />
	sealed class JsonTrackingContext : IJsonTrackingContext
	{
		/// <inheritdoc />
		public bool Active
		{
			get => active;
			set
			{
				active = true;
				logger.LogDebug("Tracking {0}activated", !active ? "de" : String.Empty);
			}
		}

		readonly IIOManager ioManager;
		readonly ICustomCommandHandler customCommandHandler;
		readonly ILogger<JsonTrackingContext> logger;
		readonly Action onDispose;

		readonly string commandsPath;
		readonly string channelsPath;

		readonly SemaphoreSlim channelsSemaphore;

		bool active;

		/// <summary>
		/// Construct a <see cref="JsonTrackingContext"/>
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="customCommandHandler">The value of <see cref="customCommandHandler"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="onDispose">The value of <see cref="onDispose"/></param>
		/// <param name="commandsPath">The value of <see cref="commandsPath"/></param>
		/// <param name="channelsPath">The value of <see cref="channelsPath"/></param>
		public JsonTrackingContext(IIOManager ioManager, ICustomCommandHandler customCommandHandler, ILogger<JsonTrackingContext> logger, Action onDispose, string commandsPath, string channelsPath)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.customCommandHandler = customCommandHandler ?? throw new ArgumentNullException(nameof(customCommandHandler));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
			this.commandsPath = commandsPath ?? throw new ArgumentNullException(nameof(commandsPath));
			this.channelsPath = channelsPath ?? throw new ArgumentNullException(nameof(channelsPath));

			channelsSemaphore = new SemaphoreSlim(1);
			active = false;

			logger.LogTrace("Created tracking context for {0} and {1}", commandsPath, channelsPath);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			logger.LogTrace("Disposing...");
			onDispose();
		}

		/// <inheritdoc />
		public async Task<IReadOnlyList<CustomCommand>> GetCustomCommands(CancellationToken cancellationToken)
		{
			try
			{
				if (Active && await ioManager.FileExists(commandsPath, cancellationToken).ConfigureAwait(false))
				{
					var resultBytes = await ioManager.ReadAllBytes(commandsPath, cancellationToken).ConfigureAwait(false);
					var resultJson = Encoding.UTF8.GetString(resultBytes);
					logger.LogTrace("Read commands JSON: {0}", resultJson);
					var result = JsonConvert.DeserializeObject<List<CustomCommand>>(resultJson, new JsonSerializerSettings
					{
						ContractResolver = new DefaultContractResolver
						{
							NamingStrategy = new SnakeCaseNamingStrategy()
						}
					});
					foreach (var I in result)
						I.SetHandler(customCommandHandler);
					return result;
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				logger.LogWarning("Error retrieving custom commands! Exception: {0}", e);
			}

			return new List<CustomCommand>();
		}

		/// <inheritdoc />
		public async Task SetChannels(IEnumerable<Channel> channels, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(channelsSemaphore, cancellationToken).ConfigureAwait(false))
			{
				var json = JsonConvert.SerializeObject(channels, new JsonSerializerSettings
				{
					ContractResolver = new CamelCasePropertyNamesContractResolver()
				});
				logger.LogTrace("Writing channels JSON: {0}", json);
				await ioManager.WriteAllBytes(channelsPath, Encoding.UTF8.GetBytes(json), cancellationToken).ConfigureAwait(false);
			}
		}
	}
}
