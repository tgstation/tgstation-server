using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Chat.Commands;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <inheritdoc />
	sealed class ChatTrackingContext : IChatTrackingContext
	{
		/// <inheritdoc />
		public bool Active
		{
			get => active;
			set
			{
				if (active == value)
					return;
				logger.LogTrace(value ? "Activated" : "Deactivated");
				active = value;
			}
		}

		/// <inheritdoc />
		public IReadOnlyCollection<ChannelRepresentation> Channels { get; private set; }

		/// <inheritdoc />
		public IEnumerable<CustomCommand> CustomCommands
		{
			get => customCommands;
			set
			{
				customCommands = (value ?? throw new InvalidOperationException("value cannot be null!"))
				.Select(customCommand =>
				{
					customCommand.SetHandler(customCommandHandler);
					return customCommand;
				})
				.ToList();
				logger.LogTrace("Custom commands set.");
			}
		}

		/// <summary>
		/// The <see cref="ICustomCommandHandler"/> for the <see cref="ChatTrackingContext"/>.
		/// </summary>
		readonly ICustomCommandHandler customCommandHandler;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ChatTrackingContext"/>.
		/// </summary>
		readonly ILogger<ChatTrackingContext> logger;

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> for modifying <see cref="onDispose"/>, <see cref="channelSink"/>, and <see cref="Channels"/>.
		/// </summary>
		readonly object synchronizationLock;

		/// <summary>
		/// Backing field for <see cref="CustomCommands"/>.
		/// </summary>
		IReadOnlyCollection<CustomCommand> customCommands;

		/// <summary>
		/// The <see cref="IChannelSink"/> if any.
		/// </summary>
		IChannelSink channelSink;

		/// <summary>
		/// The <see cref="Action"/> to run when <see cref="Dispose"/>d.
		/// </summary>
		Action onDispose;

		/// <summary>
		/// Backing field for <see cref="Active"/>.
		/// </summary>
		bool active;

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatTrackingContext"/> <see langword="class"/>.
		/// </summary>
		/// <param name="customCommandHandler">The value of <see cref="customCommandHandler"/>.</param>
		/// <param name="initialChannels">The initial value of <see cref="Channels"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="onDispose">The value of <see cref="onDispose"/>.</param>
		public ChatTrackingContext(
			ICustomCommandHandler customCommandHandler,
			IEnumerable<ChannelRepresentation> initialChannels,
			ILogger<ChatTrackingContext> logger,
			Action onDispose)
		{
			this.customCommandHandler = customCommandHandler ?? throw new ArgumentNullException(nameof(customCommandHandler));
			Channels = initialChannels?.ToList() ?? throw new ArgumentNullException(nameof(initialChannels));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));

			synchronizationLock = new object();
			Active = true;
			customCommands = Array.Empty<CustomCommand>();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			lock (synchronizationLock)
			{
				onDispose?.Invoke();
				onDispose = null;
			}
		}

		/// <inheritdoc />
		public void SetChannelSink(IChannelSink channelSink)
		{
			if (channelSink == null)
				throw new ArgumentNullException(nameof(channelSink));

			lock (synchronizationLock)
			{
				if (this.channelSink != null)
					throw new InvalidOperationException("channelSink already set!");

				this.channelSink = channelSink;
			}
		}

		/// <inheritdoc />
		public Task UpdateChannels(IEnumerable<ChannelRepresentation> newChannels, CancellationToken cancellationToken)
		{
			logger.LogTrace("UpdateChannels...");
			var completed = newChannels.ToList();
			Task updateTask;
			lock (synchronizationLock)
			{
				Channels = completed;
				updateTask = channelSink?.UpdateChannels(newChannels, cancellationToken) ?? Task.CompletedTask;
			}

			return updateTask;
		}
	}
}