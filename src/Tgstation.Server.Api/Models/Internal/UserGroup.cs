namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents a group of users.
	/// </summary>
	public class UserGroup : NamedEntity
	{
		/// <inheritdoc />
		[RequestOptions(FieldPresence.Required)]
		public override long? Id
		{
			get => base.Id;
			set => base.Id = value;
		}

		/// <summary>
		/// The <see cref="Models.PermissionSet"/> of the <see cref="UserGroup"/>.
		/// </summary>
		public PermissionSet? PermissionSet { get; set; }
	}
}
