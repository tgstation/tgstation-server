using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// A user registered in the server.
	/// </summary>
	public sealed class User : NamedEntity
	{
		/// <summary>
		/// If the <see cref="User"/> is enabled since users cannot be deleted. System users cannot be disabled.
		/// </summary>
		public bool Enabled { get; }

		/// <summary>
		/// The user's canonical (Uppercase) name.
		/// </summary>
		public string CanonicalName { get; }

		/// <summary>
		/// When the <see cref="User"/> was created.
		/// </summary>
		public DateTimeOffset CreatedAt { get; }

		/// <summary>
		/// The SID/UID of the <see cref="User"/> on Windows/POSIX respectively.
		/// </summary>
		public string? SystemIdentifier { get; }

		/// <summary>
		/// The <see cref="Entity.Id"/> of the <see cref="CreatedBy"/> <see cref="User"/>.
		/// </summary>
		readonly long? createdById;

		/// <summary>
		/// The <see cref="Entity.Id"/> of the <see cref="Group"/>.
		/// </summary>
		readonly long? groupId;

		/// <summary>
		/// Initializes a new instance of the <see cref="User"/> class.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/>.</param>
		/// <param name="name">The <see cref="NamedEntity.Name"/>.</param>
		/// <param name="canonicalName">The value of <see cref="CanonicalName"/>.</param>
		/// <param name="systemIdentifier">The value of <see cref="SystemIdentifier"/>.</param>
		/// <param name="createdAt">The value of <see cref="CreatedAt"/>.</param>
		/// <param name="createdById">The value of <see cref="createdById"/>.</param>
		/// <param name="groupId">The value of <see cref="groupId"/>.</param>
		/// <param name="enabled">The value of <see cref="Enabled"/>.</param>
		public User(
			long id,
			string name,
			string canonicalName,
			string? systemIdentifier,
			DateTimeOffset createdAt,
			long? createdById,
			long? groupId,
			bool enabled)
			: base(id, name)
		{
			SystemIdentifier = systemIdentifier;
			CanonicalName = canonicalName ?? throw new ArgumentNullException(nameof(canonicalName));
			CreatedAt = createdAt;
			this.createdById = createdById;
			Enabled = enabled;
			this.groupId = groupId;
		}

		/// <summary>
		/// The <see cref="User"/> who created this <see cref="User"/>.
		/// </summary>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="User"/> who created this <see cref="User"/>, if any.</returns>
		public ValueTask<User?> CreatedBy()
			=> throw new NotImplementedException();

		/// <summary>
		/// List of <see cref="OAuthConnection"/>s associated with the user if OAuth is configured.
		/// </summary>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="List{T}"/> of <see cref="OAuthConnection"/>s for the <see cref="User"/> if OAuth is configured.</returns>
		public ValueTask<List<OAuthConnection>>? OAuthConnections()
			=> throw new NotImplementedException();

		/// <summary>
		/// The <see cref="Types.PermissionSet"/> directly associated with the <see cref="User"/>, if any.
		/// </summary>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="Types.PermissionSet"/> directly associated with the <see cref="User"/>, if any.</returns>
		public ValueTask<PermissionSet?> PermissionSet()
			=> throw new NotImplementedException();

		/// <summary>
		/// The <see cref="UserGroup"/> asociated with the user, if any.
		/// </summary>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="UserGroup"/> associated with the <see cref="User"/>, if any.</returns>
		public ValueTask<UserGroup?> Group()
			=> throw new NotImplementedException();
	}
}
