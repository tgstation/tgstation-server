using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class ChatBot : Api.Models.Internal.ChatBot, IApiTransformable<Api.Models.ChatBot>
	{
		/// <summary>
		/// Default for <see cref="Api.Models.Internal.ChatBot.ChannelLimit"/>.
		/// </summary>
		public const ushort DefaultChannelLimit = 100;

		/// <summary>
		/// The instance <see cref="Api.Models.EntityId.Id"/>
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The parent <see cref="Models.Instance"/>
		/// </summary>
		[Required]
		public Instance Instance { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.ChatBot.Channels"/>
		/// </summary>
		public ICollection<ChatChannel> Channels { get; set; }

		/// <inheritdoc />
		public Api.Models.ChatBot ToApi() => new Api.Models.ChatBot
		{
			Channels = Channels.Select(x => x.ToApi()).ToList(),
			ConnectionString = ConnectionString,
			Enabled = Enabled,
			Provider = Provider,
			Id = Id,
			Name = Name,
			ChannelLimit = ChannelLimit,
			ReconnectionInterval = ReconnectionInterval
		};
	}
}
