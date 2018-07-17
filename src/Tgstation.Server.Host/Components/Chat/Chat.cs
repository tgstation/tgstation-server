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
		/// <summary>
		/// The <see cref="IProviderFactory"/> for the <see cref="Chat"/>
		/// </summary>
		readonly IProviderFactory providerFactory;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="Chat"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// <see cref="Command"/>s that never change
		/// </summary>
		readonly IReadOnlyList<Command> builtinCommands;

		/// <summary>
		/// Map of <see cref="IProvider"/>s in use, keyed by <see cref="ChatSettings.Id"/>
		/// </summary>
		readonly Dictionary<long, IProvider> providers;

		/// <summary>
		/// Map of <see cref="Channel.Id"/>s to <see cref="ChannelMapping"/>s
		/// </summary>
		readonly Dictionary<long, ChannelMapping> mappedChannels;

		/// <summary>
		/// The active <see cref="IJsonTrackingContext"/>s for the <see cref="Chat"/>
		/// </summary>
		readonly List<IJsonTrackingContext> trackingContexts;

		/// <summary>
		/// The <see cref="ICustomCommandHandler"/> for the <see cref="Chat"/>
		/// </summary>
		ICustomCommandHandler customCommandHandler;

		/// <summary>
		/// Used for remapping <see cref="Channel.Id"/>s
		/// </summary>
		long channelIdCounter;

		/// <summary>
		/// If <see cref="StartAsync(CancellationToken)"/> has been called
		/// </summary>
		bool started; 

		/// <summary>
		/// Construct a <see cref="Chat"/>
		/// </summary>
		/// <param name="providerFactory">The value of <see cref="providerFactory"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="commandFactory">The <see cref="ICommandFactory"/> used to populate <see cref="builtinCommands"/></param>
		public Chat(IProviderFactory providerFactory, IIOManager ioManager, ICommandFactory commandFactory)
		{
			this.providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			builtinCommands = commandFactory?.GenerateCommands() ?? throw new ArgumentNullException(nameof(commandFactory));

			providers = new Dictionary<long, IProvider>();
			mappedChannels = new Dictionary<long, ChannelMapping>();
			trackingContexts = new List<IJsonTrackingContext>();
			channelIdCounter = 1;
		}

		/// <inheritdoc />
		public void Dispose()
		{
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
				ProviderChannelId = y.Id,
				ProviderId = connectionId,
				Channel = y
			});

			long baseId;
			lock (this)
			{
				baseId = channelIdCounter;
				channelIdCounter += results.Count;
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
					I.Channel.Id = newId;
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
		public Task SendMessage(string message, IEnumerable<long> channelIds, CancellationToken cancellationToken)
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
			List<long> wdChannels;
			lock (mappedChannels)   //so it doesn't change while we're using it
				wdChannels = mappedChannels.Where(x => x.Value.IsWatchdogChannel).Select(x => x.Key).ToList();
			return SendMessage(message, wdChannels, cancellationToken);
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			await Task.WhenAll(providers.Select(x => x.Value).Select(x => x.Connect(cancellationToken))).ConfigureAwait(false);
			started = true;
		}

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => Task.WhenAll(providers.Select(x => x.Value).Select(x => x.Disconnect(cancellationToken)));

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
