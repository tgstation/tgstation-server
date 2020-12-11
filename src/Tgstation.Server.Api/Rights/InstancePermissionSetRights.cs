using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for an <see cref="Models.Instance"/>
	/// </summary>
	[Flags]
	public enum InstancePermissionSetRights : ulong
	{
		/// <summary>
		/// User has no rights/
		/// </summary>
		None = 0,

		/// <summary>
		/// Allow read access to all <see cref="Models.InstancePermissionSet"/>s in the <see cref="Models.Instance"/>.
		/// </summary>
		Read = 1,

		/// <summary>
		/// Allow write and delete access to all <see cref="Models.InstancePermissionSet"/> for the <see cref="Models.Instance"/>.
		/// </summary>
		Write = 2,

		/// <summary>
		/// Allow adding additional <see cref="Models.InstancePermissionSet"/>s to the <see cref="Models.Instance"/>.
		/// </summary>
		Create = 4
	}
}
