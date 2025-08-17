using System;

using Tgstation.Server.Host.GraphQL.Types;

namespace Tgstation.Server.Host.GraphQL.Transformers
{
	/// <summary>
	/// <see cref="Models.ITransformer{TInput, TOutput}"/> for <see cref="User"/>s.
	/// </summary>
	sealed class UserTransformer : Models.TransformerBase<Models.User, User>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserTransformer"/> class.
		/// </summary>
		public UserTransformer()
			: base(
				  BuildSubProjection<
					  Models.UserGroup,
					  Models.PermissionSet,
					  UserGroup,
					  PermissionSet,
					  UserGroupTransformer,
					  PermissionSetTransformer>(
					  (model, group, ownedPermissionSet) => new User
					  {
						  CreatedAt = model.CreatedAt ?? NotNullFallback<DateTimeOffset>(),
						  CanonicalName = model.CanonicalName ?? NotNullFallback<string>(),
						  CreatedById = model.CreatedById ?? NotNullFallback<long>(),
						  Enabled = model.Enabled ?? NotNullFallback<bool>(),
						  Id = model.Id!.Value,
						  Name = model.Name ?? NotNullFallback<string>(),
						  SystemIdentifier = model.SystemIdentifier,
						  OwnedPermissionSet = ownedPermissionSet,
						  EffectivePermissionSet = ownedPermissionSet ??
							(group != null
								? group.PermissionSet
								: NotNullFallback<PermissionSet>()),
						  Group = group,
					  },
					  model => model.Group,
					  model => model.PermissionSet))
		{
		}
	}
}
