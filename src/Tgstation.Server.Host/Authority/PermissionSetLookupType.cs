namespace Tgstation.Server.Host.Authority
{
	/// <summary>
	/// Indicates the type of <see cref="Api.Models.EntityId.Id"/> to lookup on a <see cref="Models.PermissionSet"/>.
	/// </summary>
	public enum PermissionSetLookupType
	{
		/// <summary>
		/// Lookup the <see cref="Api.Models.EntityId.Id"/> of the <see cref="Models.PermissionSet"/>.
		/// </summary>
		Id,

		/// <summary>
		/// Lookup the <see cref="Api.Models.EntityId.Id"/> of the <see cref="Models.PermissionSet.User"/>.
		/// </summary>
		UserId,

		/// <summary>
		/// Lookup the <see cref="Api.Models.EntityId.Id"/> of the <see cref="Models.PermissionSet.Group"/>.
		/// </summary>
		GroupId,
	}
}
