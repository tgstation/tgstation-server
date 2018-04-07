namespace Tgstation.Server.Host.Models
{
    sealed class CompileJob : Api.Models.CompileJob
    {
		new User TriggeredBy { get; set; }
    }
}
