using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	sealed class DreamMakerSettings : Api.Models.Internal.DreamMakerSettings
	{
		long Id { get; set; }
		long InstanceId { get; set; }
		[Required]
		Instance Instance { get; set; }
	}
}
