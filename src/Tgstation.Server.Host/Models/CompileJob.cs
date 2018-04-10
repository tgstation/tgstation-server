using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
    sealed class CompileJob : Api.Models.Internal.CompileJob
    {
		[Required]
		public User TriggeredBy { get; set; }

		[Required]
		public RevisionInformation RevisionInformation { get; set; }
    }
}
