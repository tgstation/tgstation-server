using System.Collections.Generic;
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
		public Instance Instance { get; set; } = null!; // recommended by EF

		/// <summary>
		/// See <see cref="Api.Models.Internal.ChatBotApiBase.Channels"/>.
		/// </summary>
		public ICollection<ChatChannel> Channels { get; set; } = null!; // recommended by EF

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
