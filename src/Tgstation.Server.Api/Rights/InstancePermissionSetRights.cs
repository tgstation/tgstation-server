using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for an <see cref="Models.Instance"/>.
	/// </summary>
	[Flags]
	public enum InstancePermissionSetRights : ulong
	{
		/// <summary>
		/// User has no rights/
		/// </summary>
		None = 0,

		/// <summary>
		/// Allow read access to all <see cref="Models.Internal.InstancePermissionSet"/>s in the <see cref="Models.Instance"/>.
		/// </summary>
		Read = 1 << 0,

		/// <summary>
		/// Allow write and delete access to all <see cref="Models.Internal.InstancePermissionSet"/> for the <see cref="Models.Instance"/>.
		/// </summary>
		Write = 1 << 1,

		/// <summary>
		/// Allow adding additional <see cref="Models.Internal.InstancePermissionSet"/>s to the <see cref="Models.Instance"/>.
		/// </summary>
		Create = 1 << 2,
	}
}
