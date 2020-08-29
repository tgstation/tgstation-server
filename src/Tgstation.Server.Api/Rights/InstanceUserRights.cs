using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for an <see cref="Models.Instance"/>
	/// </summary>
	[Flags]
	public enum InstanceUserRights : ulong
	{
		/// <summary>
		/// User has no rights/
		/// </summary>
		None = 0,

		/// <summary>
		/// Allow read access to all <see cref="Models.InstanceUser"/>s in the <see cref="Models.Instance"/>.
		/// </summary>
		ReadUsers = 1,

		/// <summary>
		/// Allow write and delete access to all <see cref="Models.InstanceUser"/> for the <see cref="Models.Instance"/>.
		/// </summary>
		WriteUsers = 2,

		/// <summary>
		/// Allow adding additional <see cref="Models.InstanceUser"/>s to the <see cref="Models.Instance"/>.
		/// </summary>
		CreateUsers = 4
	}
}
