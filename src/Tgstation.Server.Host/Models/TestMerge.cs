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
	}
}
