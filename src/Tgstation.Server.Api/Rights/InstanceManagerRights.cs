using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for managing <see cref="Models.Instance"/>s.
	/// </summary>
	[Flags]
	public enum InstanceManagerRights : ulong
	{
		/// <summary>
		/// User has no rights.
		/// </summary>
		None = 0,

		/// <summary>
		/// User can view <see cref="Models.Instance"/>s which they have an <see cref="Models.Internal.InstancePermissionSet"/> for.
		/// </summary>
		Read = 1 << 0,

		/// <summary>
		/// User can create <see cref="Models.Instance"/>s.
		/// </summary>
		Create = 1 << 1,

		/// <summary>
		/// User can rename <see cref="Models.Instance"/>s they can view.
		/// </summary>
		Rename = 1 << 2,

		/// <summary>
		/// User can relocate <see cref="Models.Instance"/>s they can view.
		/// </summary>
		Relocate = 1 << 3,

		/// <summary>
		/// User can online <see cref="Models.Instance"/>s they can view.
		/// </summary>
		SetOnline = 1 << 4,

		/// <summary>
		/// User can delete <see cref="Models.Instance"/>s they can view.
		/// </summary>
		Delete = 1 << 5,

		/// <summary>
		/// User can view all <see cref="Models.Instance"/>s.
		/// </summary>
		List = 1 << 6,

		/// <summary>
		/// User can change <see cref="Models.Instance.ConfigurationType"/> on instances they can view.
		/// </summary>
		SetConfiguration = 1 << 7,

		/// <summary>
		/// User can change <see cref="Models.Instance.AutoUpdateInterval"/> on instances they can view.
		/// </summary>
		SetAutoUpdate = 1 << 8,

		/// <summary>
		/// User can change <see cref="Models.Instance.ChatBotLimit"/> on instances they can view.
		/// </summary>
		SetChatBotLimit = 1 << 9,

		/// <summary>
		/// User can give themselves or their group full <see cref="InstancePermissionSetRights"/> on ALL instances.
		/// </summary>
		GrantPermissions = 1 << 10,

		/// <summary>
		/// User can change <see cref="Models.Instance.AutoStartCron"/>.
		/// </summary>
		SetAutoStart = 1 << 11,

		/// <summary>
		/// User can change <see cref="Models.Instance.AutoStopCron"/>.
		/// </summary>
		SetAutoStop = 1 << 12,
	}
}
