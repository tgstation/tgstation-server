using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
    sealed class DreamDaemonSettings : Api.Models.Internal.DreamDaemonSettings
    {
		long Id { get; set; }
		
		int? ProcessId { get; set; }

		long InstanceId { get; set; }

		[Required]
		Instance Instance { get; set; }
    }
}
