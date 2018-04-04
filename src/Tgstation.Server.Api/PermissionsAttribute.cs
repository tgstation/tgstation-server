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
		/// Right required to write the field with an Update call
		/// </summary>
		public object WriteRight
		{
			get => writeRight;
			set
			{
				if (DenyWrite)
					throw new InvalidOperationException("Cannot set WriteRight on a PermissionsAttribute with DenyRight set");
				if (ComplexWrite)
					throw new InvalidOperationException("Cannot set WriteRight on a PermissionsAttribute with ComplexWrite set");
				writeRight = value;
			}
		}

		/// <summary>
		/// If the field cannot be written to
		/// </summary>
		public bool DenyWrite
		{
			get => denyWrite;
			set
			{
				if (WriteRight != null)
					throw new InvalidOperationException("Cannot set DenyRight on a PermissionsAttribute with WriteRight set");
				if (ComplexWrite)
					throw new InvalidOperationException("Cannot set DenyRight on a PermissionsAttribute with ComplexWrite set");
				denyWrite = value;
			}
		}

		/// <summary>
		/// If the field has multiple write permissions
		/// </summary>
		public bool ComplexWrite
		{
			get => complexWrite;
			set
			{
				if (WriteRight != null)
					throw new InvalidOperationException("Cannot set ComplexWrite on a PermissionsAttribute with WriteRight set");
				if (ComplexWrite)
					throw new InvalidOperationException("Cannot set ComplexWrite on a PermissionsAttribute with DenyWrite set");
				complexWrite = value;
			}
		}

		/// <summary>
		/// Backing field for <see cref="WriteRight"/>
		/// </summary>
		object writeRight;
		/// <summary>
		/// Backing field for <see cref="DenyWrite"/>
		/// </summary>
		bool denyWrite;
		/// <summary>
		/// Backing field for <see cref="ComplexWrite"/>
		/// </summary>
		bool complexWrite;
	}
}
