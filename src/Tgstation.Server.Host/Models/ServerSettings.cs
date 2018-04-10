using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	sealed class ServerSettings : Api.Models.Internal.ServerSettings
	{
		public long Id { get; set; }
	}
}
