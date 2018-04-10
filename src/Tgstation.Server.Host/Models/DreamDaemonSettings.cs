using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
    sealed class DreamDaemonSettings : Api.Models.Internal.DreamDaemonSettings
    {
		public long Id { get; set; }

		public int? ProcessId { get; set; }

		public string AccessToken { get; set; }

		public long InstanceId { get; set; }

		[Required]
		public Instance Instance { get; set; }

		public CompileJob CompileJob { get; set; }
    }
}
