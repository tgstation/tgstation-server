namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class TestMerge : Internal.TestMerge
	{
		/// <summary>
		/// The <see cref="User"/> who created the <see cref="TestMerge"/>
		/// </summary>
		public User? MergedBy { get; set; }
	}
}