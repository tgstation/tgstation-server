using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	sealed class Job : Api.Models.Internal.Job
	{
		[Required]
		public User StartedBy { get; set; }

		[Required]
		public Instance Instance { get; set; }
	}
}
