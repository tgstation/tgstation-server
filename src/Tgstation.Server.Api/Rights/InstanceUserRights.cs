using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for an <see cref="Models.Instance"/>
	/// </summary>
	[Flags]
	public enum InstanceUserRights
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,
        /// <summary>
        /// Allow read access to <see cref="Models.InstanceUser"/> for the <see cref="Models.Instance"/>
        /// </summary>
        ReadUsers = 1,
        /// <summary>
        /// Allow write access to <see cref="Models.InstanceUser"/> for the <see cref="Models.Instance"/>
        /// </summary>
        WriteUsers = 2,
		/// <summary>
		/// User can read configuration files if the instance allows it
		/// </summary>
		ReadConfiguration = 4,
		/// <summary>
		/// Users can write configuration files if the instance allows it
		/// </summary>
		EditConfiguration = 8,
    }
}
