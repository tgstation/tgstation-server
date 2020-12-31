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
		/// User has no rights.
		/// </summary>
		None = 0,

		/// <summary>
		/// User can view <see cref="Models.Instance"/>s which they have an <see cref="Models.InstancePermissionSet"/> for.
		/// </summary>
		Read = 1,

		/// <summary>
		/// User can create <see cref="Models.Instance"/>s.
		/// </summary>
		Create = 2,

		/// <summary>
		/// User can rename <see cref="Models.Instance"/>s they can view.
		/// </summary>
		Rename = 4,

		/// <summary>
		/// User can relocate <see cref="Models.Instance"/>s they can view.
		/// </summary>
		Relocate = 8,

		/// <summary>
		/// User can online <see cref="Models.Instance"/>s they can view.
		/// </summary>
		SetOnline = 16,

		/// <summary>
		/// User can delete <see cref="Models.Instance"/>s they can view.
		/// </summary>
		Delete = 32,

		/// <summary>
		/// User can view all <see cref="Models.Instance"/>s.
		/// </summary>
		List = 64,

		/// <summary>
		/// User can change <see cref="Models.Instance.ConfigurationType"/> on instances they can view.
		/// </summary>
		SetConfiguration = 128,

		/// <summary>
		/// User can change <see cref="Models.Instance.AutoUpdateInterval"/> on instances they can view.
		/// </summary>
		SetAutoUpdate = 256,

		/// <summary>
		/// User can change <see cref="Models.Instance.ChatBotLimit"/> on instances they can view.
		/// </summary>
		SetChatBotLimit = 512,

		/// <summary>
		/// User can give themselves or their group full <see cref="Models.InstancePermissionSet"/> rights on ALL instances.
		/// </summary>
		GrantPermissions = 1024,
	}
}
