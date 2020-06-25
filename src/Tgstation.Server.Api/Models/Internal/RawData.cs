namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents raw bytes.
	/// </summary>
	public abstract class RawData
	{
		/// <summary>
		/// The bytes of the <see cref="RawData"/>.
		/// </summary>
#pragma warning disable CA1819, SA1011 // Properties should not return arrays, Closing square bracket should be followed by a space
		public byte[]? Content { get; set; }
#pragma warning restore CA1819, SA1011 // Properties should not return arrays, Closing square bracket should be followed by a space
	}
}
