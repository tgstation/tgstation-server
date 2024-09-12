using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using HotChocolate.Types;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a group of <see cref="User"/>s.
	/// </summary>
	public sealed class UserGroup : NamedEntity
	{
		/// <summary>
		/// The <see cref="Entity.Id"/> of the <see cref="PermissionSet"/>.
		/// </summary>
		readonly long permissionSetId;

		/// <summary>
		/// Initializes a new instance of the <see cref="UserGroup"/> class.
		/// </summary>
		/// <param name="id">The <see cref="Entity.Id"/>.</param>
		/// <param name="name">The <see cref="NamedEntity.Name"/>.</param>
		/// <param name="permissionSetId">The value of <see cref="permissionSetId"/>.</param>
		[SetsRequiredMembers]
		public UserGroup(
			long id,
			string name,
			long permissionSetId)
			: base(id, name)
		{
			this.permissionSetId = permissionSetId;
		}

		/// <summary>
		/// The <see cref="PermissionSet"/> of the <see cref="UserGroup"/>.
		/// </summary>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="Types.PermissionSet"/> for the <see cref="UserGroup"/>.</returns>
		public ValueTask<PermissionSet> PermissionSet()
			=> throw new NotImplementedException();

		/// <summary>
		/// Gets the <see cref="User"/>s in the <see cref="UserGroup"/>.
		/// </summary>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="List{T}"/> of <see cref="User"/>s in the <see cref="UserGroup"/>.</returns>
		[UsePaging(IncludeTotalCount = true)]
		public List<User> Users()
			=> throw new NotImplementedException();
	}
}
