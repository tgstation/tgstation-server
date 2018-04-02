using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for an <see cref="Models.Instance"/>
	/// </summary>
	[Flags]
	public enum InstanceRights
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,
		/// <summary>
		/// Allow access to <see cref="Models.Byond"/>
		/// </summary>
		Byond = 1,
	}
}
