using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
    sealed class DbCompileJob : Api.Models.CompileJob
    {
		[Required]
		new public DbUser TriggeredBy { get; set; }

		[Required]
		new public RevisionInformation RevisionInformation { get; set; }
    }
}
