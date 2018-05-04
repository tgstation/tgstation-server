namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class CompileJob : Api.Models.Internal.CompileJob
    {
		/// <summary>
		/// See <see cref="Api.Models.CompileJob.TriggeredBy"/>
		/// </summary>
		public User TriggeredBy { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.CompileJob.RevisionInformation"/>
		/// </summary>
		public RevisionInformation RevisionInformation { get; set; }
    }
}
