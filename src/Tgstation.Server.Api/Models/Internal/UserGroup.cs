namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents a group of <see cref="Models.User"/>s.
	/// </summary>
	public class UserGroup : UserGroupBase
	{
		/// <summary>
		/// The <see cref="Models.PermissionSet"/> of the <see cref="UserGroup"/>.
		/// </summary>
		public PermissionSet? PermissionSet { get; set; }
	}
}
