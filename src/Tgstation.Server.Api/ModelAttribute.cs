using System;
using Tgstation.Server.Api.Rights;

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
		/// The <see cref="Rights.RightsType"/> the model uses
		/// </summary>
		public RightsType RightsType { get; }

		/// <summary>
		/// Construct a <see cref="ModelAttribute"/>
		/// </summary>
		public ModelAttribute() => CanList = true;

		/// <summary>
		/// Construct a <see cref="ModelAttribute"/> with a given <paramref name="rightsType"/>
		/// </summary>
		/// <param name="rightsType">The value of <see cref="RightsType"/></param>
		public ModelAttribute(RightsType rightsType) => RightsType = rightsType;
	}
}
