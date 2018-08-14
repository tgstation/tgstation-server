using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class ChatBot : Api.Models.Internal.ChatBot, IApiConvertable<Api.Models.ChatBot>
	{		
		/// <summary>
		/// The <see cref="Api.Models.Instance.Id"/>
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
		public List<ChatChannel> Channels { get; set; }

		/// <inheritdoc />
		public Api.Models.ChatBot ToApi() => new Api.Models.ChatBot
		{
			Channels = Channels.Select(x => x.ToApi()).ToList(),
			ConnectionString = ConnectionString,
			Enabled = Enabled,
			Provider = Provider,
			Id = Id,
			Name = Name
		};
	}
}
