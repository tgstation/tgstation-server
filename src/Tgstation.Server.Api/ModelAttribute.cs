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
		/// Right required to read the model
		/// </summary>
		public object ReadRight { get; set; }

		/// <summary>
		/// Right required to update the model
		/// </summary>
		public object WriteRight { get; set; }

		/// <summary>
		/// If the Create and Delete actions are available for this model
		/// </summary>
		public bool CanCrud { get; set; }

		/// <summary>
		/// If the List action is available for this model
		/// </summary>
		public bool CanList { get; set; }

		/// <summary>
		/// If the actions require an <see cref="Models.Instance.Id"/>
		/// </summary>
		public bool RequiresInstance { get; set; }

		/// <summary>
		/// The enum type that designates access to the model. Must be an <see cref="Enum"/> with the <see cref="FlagsAttribute"/>
		/// </summary>
		public Type RightsEnum { get; }

		/// <summary>
		/// Construct a <see cref="ModelAttribute"/>
		/// </summary>
		public ModelAttribute() => CanList = true;

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
	}
}
