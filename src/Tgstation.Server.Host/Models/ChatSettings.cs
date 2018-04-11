using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	sealed class ChatSettings : Api.Models.Internal.ChatSettings
	{
		/// <summary>
		/// The row Id
		/// </summary>
		public long Id { get; set; }
		
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
		/// See <see cref="Api.Models.ChatSettings.AdminChannels"/>
		/// </summary>
		public List<ChatChannel> AdminChannels { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.ChatSettings.GeneralChannels"/>
		/// </summary>
		public List<ChatChannel> GeneralChannels { get; set; }
	}
}
