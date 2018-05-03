using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class TestMerge : Api.Models.Internal.TestMerge
	{
		/// <summary>
		/// See <see cref="Api.Models.TestMerge.MergedBy"/>
		/// </summary>
		[Required]
		public User MergedBy { get; set; }
		
		/// <summary>
		/// The <see cref="Models.RevisionInformation"/> for the <see cref="TestMerge"/>
		/// </summary>
		public RevisionInformation RevisionInformation { get; set; }
	}
}
