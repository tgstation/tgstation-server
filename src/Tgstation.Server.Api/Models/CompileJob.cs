namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class CompileJob : Internal.CompileJob
	{
		/// <summary>
		/// The <see cref="User"/> that triggered the job
		/// </summary>
		public User TriggeredBy { get; set; }

		/// <summary>
		/// Git revision the compiler ran on. Not modifiable
		/// </summary>
		public RevisionInformation RevisionInformation { get; set; }
	}
}
