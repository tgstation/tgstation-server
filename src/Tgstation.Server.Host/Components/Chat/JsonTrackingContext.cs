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
		readonly IIOManager ioManager;
		readonly ICustomCommandHandler customCommandHandler;
		readonly Action onDispose;

		readonly string commandsPath;
		readonly string channelsPath;

		readonly SemaphoreSlim channelsSemaphore;

		public JsonTrackingContext(IIOManager ioManager, ICustomCommandHandler customCommandHandler, Action onDispose, string commandsPath, string channelsPath)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.customCommandHandler = customCommandHandler ?? throw new ArgumentNullException(nameof(customCommandHandler));
			this.onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
			this.commandsPath = commandsPath ?? throw new ArgumentNullException(nameof(commandsPath));
			this.channelsPath = channelsPath ?? throw new ArgumentNullException(nameof(channelsPath));

			channelsSemaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose() => onDispose();

		/// <inheritdoc />
		public async Task<IReadOnlyList<CustomCommand>> GetCustomCommands(CancellationToken cancellationToken)
		{
			try
			{
				var resultBytes = await ioManager.ReadAllBytes(commandsPath, cancellationToken).ConfigureAwait(false);
				var resultJson = Encoding.UTF8.GetString(resultBytes);
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
			catch
			{
				return new List<CustomCommand>();
			}
		}

		/// <inheritdoc />
		public async Task SetChannels(IEnumerable<Channel> channels, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(channelsSemaphore, cancellationToken).ConfigureAwait(false))
				await ioManager.WriteAllBytes(channelsPath, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(channels, Formatting.Indented, new JsonSerializerSettings
				{
					ContractResolver = new DefaultContractResolver
					{
						NamingStrategy = new CamelCaseNamingStrategy()
					}
				})), cancellationToken).ConfigureAwait(false);
		}
	}
}
