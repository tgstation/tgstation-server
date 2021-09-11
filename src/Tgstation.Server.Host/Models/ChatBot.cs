using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class ChatBot : Api.Models.Internal.ChatBotSettings, IApiTransformable<ChatBotResponse>
	{
		/// <summary>
		/// Default for <see cref="Api.Models.Internal.ChatBotSettings.ChannelLimit"/>.
		/// </summary>
		public const ushort DefaultChannelLimit = 100;

		/// <summary>
		/// Backing field for <see cref="Instance"/>.
		/// </summary>
		Instance? instance;

		/// <summary>
		/// Backing field for <see cref="Channels"/>.
		/// </summary>
		ICollection<ChatChannel>? channels;

		/// <summary>
		/// The instance <see cref="Api.Models.EntityId.Id"/>.
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The parent <see cref="Models.Instance"/>.
		/// </summary>
		[Required]
		[BackingField(nameof(instance))]
		public Instance Instance
		{
			get => instance ?? throw new InvalidOperationException("Property not initialized!");
			set => instance = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.ChatBotApiBase.Channels"/>.
		/// </summary>
		[BackingField(nameof(channels))]
		public ICollection<ChatChannel> Channels
		{
			get => channels ?? throw new InvalidOperationException("Property not initialized!");
			set => channels = value;
		}

		/// <inheritdoc />
		public ChatBotResponse ToApi() => new ChatBotResponse
		{
			Channels = Channels.Select(x => x.ToApi()).ToList(),
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
