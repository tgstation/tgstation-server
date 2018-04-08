using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
    sealed class DreamDaemonSettings : Api.Models.Internal.DreamDaemonSettings
    {
		public long Id { get; set; }

		public int? ProcessId { get; set; }

		public long InstanceId { get; set; }

		public string AccessToken { get; set; }

		[Required]
		public Instance Instance { get; set; }

		new public DbCompileJob CompileJob { get; set; }
    }
}
