using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class ChatSettings : Api.Models.Internal.ChatSettings
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
		/// See <see cref="Api.Models.ChatSettings.Channels"/>
		/// </summary>
		public List<ChatChannel> Channels { get; set; }
	}
}
