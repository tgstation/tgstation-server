using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Many to many relationship for <see cref="Models.RevisionInformation"/> and <see cref="Models.TestMerge"/>.
	/// </summary>
	public sealed class RevInfoTestMerge
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="Models.TestMerge"/>.
		/// </summary>
		[Required]
		public TestMerge TestMerge { get; set; }

		/// <summary>
		/// The <see cref="Models.RevisionInformation"/>.
		/// </summary>
		[Required]
		public RevisionInformation RevisionInformation { get; set; }
	}
}
