using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
    sealed class CompileJob : Api.Models.Internal.CompileJob
    {
		/// <summary>
		/// See <see cref="Api.Models.CompileJob.TriggeredBy"/>
		/// </summary>
		[Required]
		public User TriggeredBy { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.CompileJob.RevisionInformation"/>
		/// </summary>
		[Required]
		public RevisionInformation RevisionInformation { get; set; }
    }
}
