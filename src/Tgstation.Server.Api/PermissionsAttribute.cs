using System;

namespace Tgstation.Server.Api
{
	/// <summary>
	/// Indicates permissions for model fields
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class PermissionsAttribute : Attribute
	{
		/// <summary>
		/// Right required to read the field
		/// </summary>
		public object ReadRight { get; set; }

		/// <summary>
		/// Right required to update the field
		/// </summary>
		public object WriteRight { get; set; }

		/// <summary>
		/// If the field cannot be written to
		/// </summary>
		public bool DenyWrite { get; set; }
	}
}
