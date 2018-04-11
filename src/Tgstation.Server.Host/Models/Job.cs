using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	sealed class Job : Api.Models.Internal.Job
	{
		/// <summary>
		/// See <see cref="Api.Models.Job.StartedBy"/>
		/// </summary>
		[Required]
		public User StartedBy { get; set; }
	}
}
