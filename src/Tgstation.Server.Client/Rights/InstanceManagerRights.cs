using System;

namespace Tgstation.Server.Client.Rights
{
	/// <summary>
	/// Rights for <see cref="Components.IInstanceManagerClient"/>
	/// </summary>
	[Flags]
	public enum InstanceManagerRights
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,
		/// <summary>
		/// User can view <see cref="Api.Models.Instance"/>s which they have any rights for
		/// </summary>
		Read = 1,
		/// <summary>
		/// User can create <see cref="Api.Models.Instance"/>s
		/// </summary>
		Create = 2,
		/// <summary>
		/// User can rename <see cref="Api.Models.Instance"/>s they can view
		/// </summary>
		Rename = 4,
		/// <summary>
		/// User can relocate <see cref="Api.Models.Instance"/>s they can view
		/// </summary>
		Relocate = 8,
		/// <summary>
		/// User can online <see cref="Api.Models.Instance"/>s they can view
		/// </summary>
		Online = 16,
		/// <summary>
		/// User can offline <see cref="Api.Models.Instance"/>s they can view
		/// </summary>
		Offline = 32,
		/// <summary>
		/// User can delete <see cref="Api.Models.Instance"/>s they can view
		/// </summary>
		Delete = 64,
		/// <summary>
		/// User can view all <see cref="Api.Models.Instance"/>s
		/// </summary>
		List = 128
	}
}
