using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class CompileJob : Api.Models.Internal.CompileJob
    {
		/// <summary>
		/// The <see cref="Api.Models.Internal.Job.Id"/> of <see cref="Job"/>
		/// </summary>
		public long JobId { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.CompileJob.Job"/>
		/// </summary>
		[Required]
		public Job Job { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.CompileJob.RevisionInformation"/>
		/// </summary>
		[Required]
		public RevisionInformation RevisionInformation { get; set; }
    }
}
