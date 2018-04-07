using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
    sealed class CompileJob : Api.Models.CompileJob
    {
		[Required]
		new User TriggeredBy { get; set; }
    }
}
