using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class TestMerge : TestMergeApiBase
	{
		/// <summary>
		/// The <see cref="NamedEntity"/> of the user who created the <see cref="TestMerge"/>.
		/// </summary>
		public UserName? MergedBy { get; set; }
	}
}
