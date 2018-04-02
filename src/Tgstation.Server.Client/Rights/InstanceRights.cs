using System;

namespace Tgstation.Server.Client.Rights
{
	/// <summary>
	/// Rights for an <see cref="Components.IInstanceClient"/>
	/// </summary>
	[Flags]
	public enum InstanceRights
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,
		/// <summary>
		/// Allow access to <see cref="Components.IByondClient"/>
		/// </summary>
		Byond = 1,
	}
}
