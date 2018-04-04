using System;

namespace Tgstation.Server.Api
{
	/// <summary>
	/// Indicates the rights <see cref="Enum"/> for a model
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class ModelAttribute : Attribute
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
				denyWrite = value;
			}
		}

		/// <summary>
		/// The enum type that designates access to the model. Must be an <see cref="Enum"/> with the <see cref="FlagsAttribute"/>
		/// </summary>
		public Type RightsEnum { get; }

		/// <summary>
		/// Construct a <see cref="ModelAttribute"/>
		/// </summary>
		/// <param name="rightsEnum">The value of <see cref="RightsEnum"/></param>
		public ModelAttribute(Type rightsEnum)
		{
			if (!typeof(Enum).IsAssignableFrom(rightsEnum))
				throw new ArgumentException("rightsEnum must be an enum type!", nameof(rightsEnum));
			if (rightsEnum.GetCustomAttributes(typeof(FlagsAttribute), false).Length == 0)
				throw new ArgumentException("rightsEnum must have the FlagsAttribute!", nameof(rightsEnum));
			if(Enum.GetUnderlyingType(rightsEnum) != typeof(int))
				throw new ArgumentException("rightsEnum must be an integer type!", nameof(rightsEnum));
			RightsEnum = rightsEnum;
		}

		/// <summary>
		/// Backing field for <see cref="WriteRight"/>
		/// </summary>
		object writeRight;
		/// <summary>
		/// Backing field for <see cref="DenyWrite"/>
		/// </summary>
		bool denyWrite;
	}
}
