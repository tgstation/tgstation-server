using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	sealed class DreamMakerSettings : Api.Models.Internal.DreamMakerSettings
	{
		public long Id { get; set; }

		public long InstanceId { get; set; }

		[Required]
		public Instance Instance { get; set; }

		public CompileJob CompileJob { get; set; }
	}
}
