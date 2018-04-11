using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class Job : Api.Models.Internal.Job
	{
		/// <summary>
		/// See <see cref="Api.Models.Job.StartedBy"/>
		/// </summary>
		[Required]
		public User StartedBy { get; set; }
	}
}
