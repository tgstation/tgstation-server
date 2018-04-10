using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	sealed class ChatSettings : Api.Models.Internal.ChatSettings
	{
		public long Id { get; set; }
		
		public long InstanceId { get; set; }

		[Required]
		public Instance Instance { get; set; }

		public List<ChatChannel> AdminChannels { get; set; }
		
		public List<ChatChannel> GeneralChannels { get; set; }
	}
}
