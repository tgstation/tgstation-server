namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Represents a field in a <see cref="ChatEmbed"/>.
	/// </summary>
	public sealed class ChatEmbedField
	{
		/// <summary>
		/// Gets the name of the field.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets the value of the field.
		/// </summary>
		public string Value { get; set; }

		/// <summary>
		/// Gets a value indicating whether the field should display inline.
		/// </summary>
		public bool? IsInline { get; set; }
	}
}
