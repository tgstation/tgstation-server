using System;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Indicates the <see cref="FieldPresence"/> for fields in models.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
	public sealed class RequestOptionsAttribute : Attribute
	{
		/// <summary>
		/// The <see cref="FieldPresence"/>.
		/// </summary>
		public FieldPresence Presence { get; }

		/// <summary>
		/// If this only applies to HTTP PUT requests with the model.
		/// </summary>
		public bool PutOnly { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RequestOptionsAttribute"/> <see langword="class"/>.
		/// </summary>
		/// <param name="presence">The value of <see cref="Presence"/>.</param>
		public RequestOptionsAttribute(FieldPresence presence)
		{
			Presence = presence;
		}
	}
}
