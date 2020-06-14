using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for managing <see cref="Models.Instance"/>s
	/// </summary>
	[Flags]
	public enum InstanceManagerRights : ulong
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,

		/// <summary>
		/// User can view <see cref="Models.Instance"/>s which they have any rights for
		/// </summary>
		Read = 1,

		/// <summary>
		/// User can create <see cref="Models.Instance"/>s
		/// </summary>
		Create = 2,

		/// <summary>
		/// User can rename <see cref="Models.Instance"/>s they can view
		/// </summary>
		Rename = 4,

		/// <summary>
		/// User can relocate <see cref="Models.Instance"/>s they can view
		/// </summary>
		Relocate = 8,

		/// <summary>
		/// User can online <see cref="Models.Instance"/>s they can view
		/// </summary>
		SetOnline = 16,

		/// <summary>
		/// User can delete <see cref="Models.Instance"/>s they can view
		/// </summary>
		Delete = 32,

		/// <summary>
		/// User can view all <see cref="Models.Instance"/>s
		/// </summary>
		List = 64,

		/// <summary>
		/// User can change <see cref="Models.Instance.ConfigurationType"/>
		/// </summary>
		SetConfiguration = 128,

		/// <summary>
		/// User can change <see cref="Models.Instance.AutoUpdateInterval"/>
		/// </summary>
		SetAutoUpdate = 256,

		/// <summary>
		/// User can change <see cref="Models.Instance.ChatBotLimit"/>.
		/// </summary>
		SetChatBotLimit = 512,

		/// <summary>
		/// User can give themselves full <see cref="Models.InstanceUser"/> rights on instances.
		/// </summary>
		GrantPermissions = 1024,
	}
}
