using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc cref="Api.Models.Internal.ChatBotSettings" />
	public sealed class ChatBot : Api.Models.Internal.ChatBotSettings, ILegacyApiTransformable<ChatBotResponse>
	{
		/// <summary>
		/// Default for <see cref="Api.Models.Internal.ChatBotSettings.ChannelLimit"/>.
		/// </summary>
		public const ushort DefaultChannelLimit = 100;

		/// <summary>
		/// The instance <see cref="Api.Models.EntityId.Id"/>.
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The parent <see cref="Models.Instance"/>.
		/// </summary>
		[Required]
		public Instance? Instance { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.Internal.ChatBotApiBase.Channels"/>.
		/// </summary>
		public ICollection<ChatChannel> Channels { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatBot"/> class.
		/// </summary>
		public ChatBot()
			: this(new List<ChatChannel>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatBot"/> class.
		/// </summary>
		/// <param name="channels">The value of <see cref="Channels"/>.</param>
		public ChatBot(ICollection<ChatChannel> channels)
		{
			Channels = channels ?? throw new ArgumentNullException(nameof(channels));
		}

		/// <inheritdoc />
		public ChatBotResponse ToApi() => new()
		{
			Channels = Channels.Select(x => x.ToApi(this.Require(x => x.Provider))).ToList(),
			ConnectionString = ConnectionString,
			Enabled = Enabled,
			Provider = Provider,
			Id = Id,
			Name = Name,
			ChannelLimit = ChannelLimit,
			ReconnectionInterval = ReconnectionInterval,
		};
	}
}
