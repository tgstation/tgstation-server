using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat.Commands;
using Tgstation.Server.Host.Components.Chat.Providers;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <inheritdoc />
	sealed class Chat : IChat
	{
		const string CommonMention = "!tgs";

		/// <summary>
		/// The <see cref="IProviderFactory"/> for the <see cref="Chat"/>
		/// </summary>
		readonly IProviderFactory providerFactory;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="Chat"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Chat"/>
		/// </summary>
		readonly ILogger<Chat> logger;

		/// <summary>
		/// Unchanging <see cref="ICommand"/>s in the <see cref="Chat"/> mapped by <see cref="ICommand.Name"/>
		/// </summary>
		readonly Dictionary<string, ICommand> builtinCommands;

		/// <summary>
		/// Map of <see cref="IProvider"/>s in use, keyed by <see cref="ChatSettings.Id"/>
		/// </summary>
		readonly Dictionary<long, IProvider> providers;

		/// <summary>
		/// Map of <see cref="Channel.RealId"/>s to <see cref="ChannelMapping"/>s
		/// </summary>
		readonly Dictionary<ulong, ChannelMapping> mappedChannels;

		/// <summary>
		/// The active <see cref="IJsonTrackingContext"/>s for the <see cref="Chat"/>
		/// </summary>
		readonly List<IJsonTrackingContext> trackingContexts;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="chatHandler"/>
		/// </summary>
		readonly CancellationTokenSource handlerCts;

		/// <summary>
		/// The <see cref="ICustomCommandHandler"/> for the <see cref="ChangeChannels(long, IEnumerable{Api.Models.ChatChannel}, CancellationToken)"/>
		/// </summary>
		ICustomCommandHandler customCommandHandler;

		/// <summary>
		/// The <see cref="Task"/> that monitors incoming chat messages
		/// </summary>
		Task chatHandler;

		/// <summary>
		/// Used for remapping <see cref="Channel.RealId"/>s
		/// </summary>
		ulong channelIdCounter;

		/// <summary>
		/// If <see cref="StartAsync(CancellationToken)"/> has been called
		/// </summary>
		bool started;

		/// <summary>
		/// Construct a <see cref="Chat"/>
		/// </summary>
		/// <param name="providerFactory">The value of <see cref="providerFactory"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="commandFactory">The <see cref="ICommandFactory"/> used to populate <see cref="builtinCommands"/></param>
		public Chat(IProviderFactory providerFactory, IIOManager ioManager, ILogger<Chat> logger, ICommandFactory commandFactory)
		{
			this.providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			builtinCommands = new Dictionary<string, ICommand>();
			foreach (var I in commandFactory?.GenerateCommands() ?? throw new ArgumentNullException(nameof(commandFactory)))
				builtinCommands.Add(I.Name, I);

			providers = new Dictionary<long, IProvider>();
			mappedChannels = new Dictionary<ulong, ChannelMapping>();
			trackingContexts = new List<IJsonTrackingContext>();
			handlerCts = new CancellationTokenSource();
			channelIdCounter = 1;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			handlerCts.Dispose();
			foreach (var I in providers)
				I.Value.Dispose();
		}

		/// <summary>
		/// Remove a <see cref="IProvider"/> from <see cref="providers"/> and <see cref="mappedChannels"/> optionally updating the <see cref="trackingContexts"/> as well
		/// </summary>
		/// <param name="connectionId">The <see cref="ChatSettings.Id"/> of the <see cref="IProvider"/> to delete</param>
		/// <param name="updateTrackings">If <see cref="trackingContexts"/> should be update</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IProvider"/> being removed if it exists, <see langword="false"/> otherwise</returns>
		async Task<IProvider> RemoveProvider(long connectionId, bool updateTrackings, CancellationToken cancellationToken)
		{
			IProvider provider;
			lock (providers)
				if (!providers.TryGetValue(connectionId, out provider))
					return null;
			Task task;
			lock (mappedChannels)
			{
				foreach (var kvp in mappedChannels.Where(x => x.Value.ProviderId == connectionId))
					mappedChannels.Remove(kvp.Key);

				if (updateTrackings)
					lock (trackingContexts)
						task = Task.WhenAll(trackingContexts.Select(x => x.SetChannels(mappedChannels.Select(y => y.Value.Channel), cancellationToken)));
				else
					task = Task.CompletedTask;
			}
			await task.ConfigureAwait(false);
			return provider;
		}

		/// <summary>
		/// Processes a <paramref name="message"/>
		/// </summary>
		/// <param name="provider">The <see cref="IProvider"/> who recevied <paramref name="message"/></param>
		/// <param name="message">The <see cref="Message"/> to process</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task ProcessMessage(IProvider provider, Message message, CancellationToken cancellationToken)
		{
			logger.LogTrace("Chat message: {0}. User (Note unconverted provider Id): {1}", message.Content, JsonConvert.SerializeObject(message.User));

			var splits = new List<string>(message.Content.Split(' '));
			var address = splits[0];
			if (address.Length > 1 && (address[address.Length - 1] == ':' || address[address.Length - 1] == ','))
				address = address.Substring(0, address.Length - 1);

			address = address.ToUpperInvariant();

			if (address != CommonMention.ToUpperInvariant() && address != provider.BotMention.ToUpperInvariant())
				//no mention
				return;

			if (splits.Count == 1)
			{
				//just a mention
				await SendMessage("Hi!", new List<ulong> { message.User.Channel.RealId }, cancellationToken).ConfigureAwait(false);
				return;
			}

			splits.RemoveAt(0);

			var command = splits[0].ToUpperInvariant();
			splits.RemoveAt(0);
			var arguments = String.Join(" ", splits);
			
			try
			{
				if (!builtinCommands.TryGetValue(command, out ICommand commandHandler))
				{
					var tasks = trackingContexts.Select(x => x.GetCustomCommands(cancellationToken));
					await Task.WhenAll(tasks).ConfigureAwait(false);
					commandHandler = tasks.SelectMany(x => x.Result).Where(x => x.Name.ToUpperInvariant() == command).FirstOrDefault();
				}

				if (command == default)
				{
					await SendMessage("Invalid command! Type '?' or 'help' for available commands.", new List<ulong> { message.User.Channel.RealId }, cancellationToken).ConfigureAwait(false);
					return;
				}

				var result = await commandHandler.Invoke(arguments, message.User, cancellationToken).ConfigureAwait(false);
				if(result != null)
					await SendMessage(result, new List<ulong> { message.User.Channel.RealId }, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				//error bc custom commands should reply about why it failed
				logger.LogError("Error processing chat command: {0}", e);
				await SendMessage("Internal error processing command!", new List<ulong> { message.User.Channel.RealId }, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Monitors active providers for new <see cref="Message"/>s
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task MonitorMessages(CancellationToken cancellationToken)
		{
			var messageTasks = new Dictionary<IProvider, Task<Message>>();
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					//prune disconnected providers
					foreach (var I in messageTasks)
						if (!I.Key.Connected)
							messageTasks.Remove(I.Key);

					//add new ones
					foreach (var I in providers)
						if (I.Value.Connected && !messageTasks.ContainsKey(I.Value))
							messageTasks.Add(I.Value, I.Value.NextMessage(cancellationToken));

					//wait for a message
					var tasks = messageTasks.Select(x => x.Value);
					await Task.WhenAny().ConfigureAwait(false);

					//process completed ones
					foreach (var I in messageTasks.Where(x => x.Value.IsCompleted))
					{
						messageTasks.Remove(I.Key);

						var message = await I.Value.ConfigureAwait(false);

						await ProcessMessage(I.Key, message, cancellationToken).ConfigureAwait(false);
					}
				}
			}
			catch (OperationCanceledException) { }
		}

		/// <inheritdoc />
		public async Task ChangeChannels(long connectionId, IEnumerable<Api.Models.ChatChannel> newChannels, CancellationToken cancellationToken)
		{
			if (newChannels == null)
				throw new ArgumentNullException(nameof(newChannels));
			var provider = await RemoveProvider(connectionId, false, cancellationToken).ConfigureAwait(false);
			if (provider == null)
				return;
			var results = await provider.MapChannels(newChannels, cancellationToken).ConfigureAwait(false);
			if (results == null)    //aborted
				return;
			var mappings = Enumerable.Zip(newChannels, results, (x, y) => new ChannelMapping
			{
				IsWatchdogChannel = x.IsWatchdogChannel,
				ProviderChannelId = y.RealId,
				ProviderId = connectionId,
				Channel = y
			});

			ulong baseId;
			lock (this)
			{
				baseId = channelIdCounter;
				channelIdCounter += (ulong)results.Count;
			}

			Task task;
			lock (mappedChannels)
			{
				lock (providers)
					if (!providers.TryGetValue(connectionId, out IProvider verify) || verify != provider)   //aborted again
						return;
				foreach (var I in mappings)
				{
					var newId = baseId++;
					mappedChannels.Add(newId, I);
					I.Channel.RealId = newId;
				}

				lock (trackingContexts)
					task = Task.WhenAll(trackingContexts.Select(x => x.SetChannels(mappedChannels.Select(y => y.Value.Channel), cancellationToken)));
			}
			await task.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task ChangeSettings(ChatSettings newSettings, CancellationToken cancellationToken)
		{
			if (newSettings == null)
				throw new ArgumentNullException(nameof(newSettings));
			IProvider provider;
			lock (providers)
			{
				//raw settings changes forces a rebuild of the provider
				if (providers.TryGetValue(newSettings.Id, out provider))
				{
					providers.Remove(newSettings.Id);
					provider.Dispose();
				}
				if (newSettings.Enabled.Value)
				{
					provider = providerFactory.CreateProvider(newSettings);
					providers.Add(newSettings.Id, provider);
				}
			}
			lock (mappedChannels)
				foreach (var channelId in mappedChannels.Where(x => x.Value.ProviderId == newSettings.Id).Select(x => x.Key))
					mappedChannels.Remove(channelId);
			if (newSettings.Enabled.Value && started)
				await provider.Connect(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task SendMessage(string message, IEnumerable<ulong> channelIds, CancellationToken cancellationToken)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));
			if (channelIds == null)
				throw new ArgumentNullException(nameof(channelIds));

			return Task.WhenAll(channelIds.Select(x =>
			{
				ChannelMapping channelMapping;
				lock(mappedChannels)
					if (!mappedChannels.TryGetValue(x, out channelMapping))
						return Task.CompletedTask;
				IProvider provider;
				lock (providers)
					if (!providers.TryGetValue(channelMapping.ProviderId, out provider))
						return Task.CompletedTask;
				return provider.SendMessage(channelMapping.ProviderChannelId, message, cancellationToken);
			}));
		}

		/// <inheritdoc />
		public Task SendWatchdogMessage(string message, CancellationToken cancellationToken)
		{
			List<ulong> wdChannels;
			lock (mappedChannels)   //so it doesn't change while we're using it
				wdChannels = mappedChannels.Where(x => x.Value.IsWatchdogChannel).Select(x => x.Key).ToList();
			return SendMessage(message, wdChannels, cancellationToken);
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			await Task.WhenAll(providers.Select(x => x.Value).Select(x => x.Connect(cancellationToken))).ConfigureAwait(false);
			chatHandler = MonitorMessages(handlerCts.Token);
			started = true;
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			handlerCts.Cancel();
			await chatHandler.ConfigureAwait(false);
			await Task.WhenAll(providers.Select(x => x.Value).Select(x => x.Disconnect(cancellationToken))).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<IJsonTrackingContext> TrackJsons(string basePath, string channelsJsonName, string commandsJsonName, CancellationToken cancellationToken)
		{
			if (customCommandHandler == null)
				throw new InvalidOperationException("RegisterCommandHandler() hasn't been called!");
			JsonTrackingContext context = null;
			context = new JsonTrackingContext(ioManager, customCommandHandler, () =>
			{
				lock (trackingContexts)
					trackingContexts.Remove(context);
			}, ioManager.ConcatPath(basePath, commandsJsonName), ioManager.ConcatPath(basePath, channelsJsonName));
			Task task;
			lock (trackingContexts)
			{
				trackingContexts.Add(context);
				lock (mappedChannels)
					task = Task.WhenAll(trackingContexts.Select(x => x.SetChannels(mappedChannels.Select(y => y.Value.Channel), cancellationToken)));
			}
			await task.ConfigureAwait(false);
			return context;
		}

		/// <inheritdoc />
		public bool Connected(long connectionId)
		{
			lock (providers)
				return providers.TryGetValue(connectionId, out var provider) && provider.Connected;
		}

		/// <inheritdoc />
		public void RegisterCommandHandler(ICustomCommandHandler customCommandHandler)
		{
			if (this.customCommandHandler != null)
				throw new InvalidOperationException("RegisterCommandHandler() already called!");
			this.customCommandHandler = customCommandHandler ?? throw new ArgumentNullException(nameof(customCommandHandler));
		}

		/// <inheritdoc />
		public Task DeleteConnection(long connectionId, CancellationToken cancellationToken) => RemoveProvider(connectionId, true, cancellationToken);
	}
}
