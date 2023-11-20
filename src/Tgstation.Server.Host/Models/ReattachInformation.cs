using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Database representation of <see cref="Components.Session.ReattachInformation"/>.
	/// </summary>
	public sealed class ReattachInformation : ReattachInformationBase
	{
		/// <summary>
		/// The <see cref="Models.CompileJob"/> for the <see cref="Components.Session.ReattachInformation.Dmb"/>.
		/// </summary>
		[Required]
		public CompileJob CompileJob { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/> of <see cref="CompileJob"/>.
		/// </summary>
		public long CompileJobId { get; set; }

		/// <summary>
		/// The <see cref="Models.CompileJob"/> the server was initially launched with in the case of Windows.
		/// </summary>
		public CompileJob InitialCompileJob { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/> of <see cref="InitialCompileJob"/>.
		/// </summary>
		public long? InitialCompileJobId { get; set; }
	}
}
