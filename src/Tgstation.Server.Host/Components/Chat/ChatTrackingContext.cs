using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Components.Chat.Commands;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <inheritdoc />
	sealed class ChatTrackingContext : DisposeInvoker, IChatTrackingContext
	{
		/// <inheritdoc />
		public bool Active
		{
			get => active && !IsDisposed;
			set
			{
				if (active == value)
					return;
				if (value)
					logger.LogTrace("Activated");
				else
					logger.LogTrace("Deactivated");

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
		/// <see langword="lock"/> <see cref="object"/> for modifying <see cref="Channels"/> and calling <see cref="IChannelSink.UpdateChannels(IEnumerable{ChannelRepresentation}, CancellationToken)"/>.
		/// </summary>
		readonly object synchronizationLock;

		/// <summary>
		/// The <see cref="IChannelSink"/> if any.
		/// </summary>
		volatile IChannelSink? channelSink;

		/// <summary>
		/// Backing field for <see cref="CustomCommands"/>.
		/// </summary>
		IReadOnlyCollection<CustomCommand> customCommands;

		/// <summary>
		/// Backing field for <see cref="Active"/>.
		/// </summary>
		bool active;

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatTrackingContext"/> class.
		/// </summary>
		/// <param name="customCommandHandler">The value of <see cref="customCommandHandler"/>.</param>
		/// <param name="initialChannels">The initial value of <see cref="Channels"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="disposeAction">The <see cref="IDisposable.Dispose"/> action for the <see cref="DisposeInvoker"/>.</param>
		public ChatTrackingContext(
			ICustomCommandHandler customCommandHandler,
			IEnumerable<ChannelRepresentation> initialChannels,
			ILogger<ChatTrackingContext> logger,
			Action disposeAction)
			: base(disposeAction)
		{
			this.customCommandHandler = customCommandHandler ?? throw new ArgumentNullException(nameof(customCommandHandler));
			Channels = initialChannels?.ToList() ?? throw new ArgumentNullException(nameof(initialChannels));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			synchronizationLock = new object();
			Active = true;
			customCommands = Array.Empty<CustomCommand>();
		}

		/// <inheritdoc />
		public void SetChannelSink(IChannelSink channelSink)
		{
			ArgumentNullException.ThrowIfNull(channelSink);

			var originalValue = Interlocked.CompareExchange(ref this.channelSink, channelSink, null);
			if (originalValue != null)
				throw new InvalidOperationException("channelSink already set!");
		}

		/// <inheritdoc />
		public ValueTask UpdateChannels(IEnumerable<ChannelRepresentation> newChannels, CancellationToken cancellationToken)
		{
			logger.LogTrace("UpdateChannels...");
			var completed = newChannels.ToList();
			ValueTask updateTask;
			lock (synchronizationLock)
			{
				Channels = completed;
				updateTask = channelSink?.UpdateChannels(newChannels, cancellationToken) ?? ValueTask.CompletedTask;
			}

			return updateTask;
		}
	}
}
