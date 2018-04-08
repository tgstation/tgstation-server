using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	sealed class RepositorySettings : Api.Models.Internal.RepositorySettings
	{
		public long Id { get; set; }

		public long InstanceId { get; set; }

		[Required]
		public Instance Instance { get; set; }
		
		new public RevisionInformation RevisionInformation { get; set; }
	}
}
