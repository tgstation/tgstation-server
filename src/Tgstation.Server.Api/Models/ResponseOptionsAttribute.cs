using System;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Indicates the response <see cref="FieldPresence"/> of API fields. Changes it from <see cref="FieldPresence.Required"/> to <see cref="FieldPresence.Optional"/> by default.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class ResponseOptionsAttribute : Attribute
	{
		/// <summary>
		/// The <see cref="FieldPresence"/>.
		/// </summary>
		public FieldPresence Presence { get; set; }
	}
}
