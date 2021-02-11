namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Indicates whether a request field is <see cref="Required"/> or <see cref="Ignored"/>.
	/// </summary>
	public enum FieldPresence
	{
		/// <summary>
		/// The field is optional
		/// </summary>
		Optional,

		/// <summary>
		/// The field is required.
		/// </summary>
		Required,

		/// <summary>
		/// The field is ignored or should not appear.
		/// </summary>
		Ignored
	}
}
