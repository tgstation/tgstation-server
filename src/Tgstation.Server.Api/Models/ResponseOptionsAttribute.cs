using System;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Indicates API fields that may be null on response.
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
