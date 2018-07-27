using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class ChatSettings : Api.Models.Internal.ChatSettings, IApiConvertable<Api.Models.ChatSettings>
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

		/// <inheritdoc />
		public Api.Models.ChatSettings ToApi() => new Api.Models.ChatSettings
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
