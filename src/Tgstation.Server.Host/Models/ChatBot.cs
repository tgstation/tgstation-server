using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
		/// <see cref="Api.Models.EntityId.Id"/>.
		/// </summary>
		[NotMapped]
		public new long Id
		{
			get => base.Id ?? throw new InvalidOperationException("Id was null!");
			set => base.Id = value;
		}

		/// <summary>
		/// <see cref="Api.Models.NamedEntity.Name"/>.
		/// </summary>
		[NotMapped]
		public new string Name
		{
			get => base.Name ?? throw new InvalidOperationException("Name was null!");
			set => base.Name = value;
		}

		/// <summary>
		/// <see cref="Api.Models.Internal.ChatBotSettings.ConnectionString"/>.
		/// </summary>
		[NotMapped]
		public new string ConnectionString
		{
			get => base.ConnectionString ?? throw new InvalidOperationException("ConnectionString was null!");
			set => base.ConnectionString = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.ChatBotSettings.Enabled"/>.
		/// </summary>
		[NotMapped]
		public new bool Enabled
		{
			get => base.Enabled ?? throw new InvalidOperationException("Enabled was null!");
			set => base.Enabled = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.ChatBotSettings.ReconnectionInterval"/>.
		/// </summary>
		[NotMapped]
		public new uint ReconnectionInterval
		{
			get => base.ReconnectionInterval ?? throw new InvalidOperationException("ReconnectionInterval was null!");
			set => base.ReconnectionInterval = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.ChatBotSettings.ChannelLimit"/>.
		/// </summary>
		[NotMapped]
		public new ushort ChannelLimit
		{
			get => base.ChannelLimit ?? throw new InvalidOperationException("ChannelLimit was null!");
			set => base.ChannelLimit = value;
		}

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
		public ChatBotResponse ToApi() => new ()
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
