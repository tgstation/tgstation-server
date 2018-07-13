using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
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
		/// Map of <see cref="IProvider"/>s in use, keyed by <see cref="ChatSettings.Id"/>
		/// </summary>
		readonly Dictionary<long, IProvider> providers;

		/// <summary>
		/// Map of <see cref="Channel.Id"/>s to <see cref="ChannelMapping"/>s
		/// </summary>
		readonly Dictionary<long, ChannelMapping> mappedChannels;

		/// <summary>
		/// Used for remapping <see cref="Channel.Id"/>s
		/// </summary>
		long channelIdCounter;

		/// <summary>
		/// Construct a <see cref="Chat"/>
		/// </summary>
		/// <param name="providerFactory">The value of <see cref="providerFactory"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		public Chat(IProviderFactory providerFactory, IIOManager ioManager)
		{
			this.providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));

			providers = new Dictionary<long, IProvider>();
			mappedChannels = new Dictionary<long, ChannelMapping>();
			channelIdCounter = 1;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			foreach (var I in providers)
				I.Value.Dispose();
		}

		/// <inheritdoc />
		public async Task ChangeChannels(long connectionId, IEnumerable<Api.Models.ChatChannel> newChannels, CancellationToken cancellationToken)
		{
			if (newChannels == null)
				throw new ArgumentNullException(nameof(newChannels));
			IProvider provider;
			lock (providers)
				if (!providers.TryGetValue(connectionId, out provider))
					return;
			var results = await provider.MapChannels(newChannels, cancellationToken).ConfigureAwait(false);
			if (results == null)    //aborted
				return;
			var mappings = Enumerable.Zip(newChannels, results, (x, y) => new ChannelMapping
			{
				IsWatchdogChannel = x.IsWatchdogChannel,
				ProviderChannelId = y.Id,
				ProviderId = connectionId
			});

			long baseId;
			lock (this)
			{
				baseId = channelIdCounter;
				channelIdCounter += results.Count;
			}
			lock (mappedChannels)
			{
				lock (providers)
					if (!providers.TryGetValue(connectionId, out IProvider verify) || verify != provider)   //aborted again
						return;
				foreach (var I in mappings)
					mappedChannels.Add(baseId++, I);
			}
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
				if (newSettings.Enabled)
				{
					provider = providerFactory.CreateProvider(newSettings);
					providers.Add(newSettings.Id, provider);
				}
			}
			lock (mappedChannels)
				foreach (var channelId in mappedChannels.Where(x => x.Value.ProviderId == newSettings.Id).Select(x => x.Key))
					mappedChannels.Remove(channelId);
			if (newSettings.Enabled)
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
		public Task StartAsync(CancellationToken cancellationToken) => Task.WhenAll(providers.Select(x => x.Value).Select(x => x.Connect(cancellationToken)));

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public Task<IJsonTrackingContext> TrackJsons(string basePath, string channelsJsonName, string commandsJsonName, CancellationToken cancellationToken)
		{
			ioManager.ResolvePath(".");
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public bool Connected(long connectionId)
		{
			lock (providers)
				return providers.TryGetValue(connectionId, out var provider) && provider.Connected;
		}
	}
}
