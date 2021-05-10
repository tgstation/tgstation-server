using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Database representation of <see cref="Components.Session.ReattachInformation"/>.
	/// </summary>
	public sealed class ReattachInformation : ReattachInformationBase
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="Models.CompileJob"/> for the <see cref="Components.Session.ReattachInformation.Dmb"/>.
		/// </summary>
		[Required]
		public CompileJob CompileJob { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/> of <see cref="CompileJob"/>.
		/// </summary>
		public long CompileJobId { get; set; }
	}
}
