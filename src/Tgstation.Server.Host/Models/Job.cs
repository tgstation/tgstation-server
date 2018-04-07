using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	sealed class Job : Api.Models.Internal.Job
	{
		[Required]
		new public User StartedBy { get; set; }

		Instance Instance { get; set; }
	}
}
