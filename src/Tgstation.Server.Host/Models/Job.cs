using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	sealed class Job : Api.Models.Internal.Job
	{
		[Required]
		new public DbUser StartedBy { get; set; }

		[Required]
		Instance Instance { get; set; }
	}
}
