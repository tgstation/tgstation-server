using System;

namespace Tgstation.Server.Host.Models.Transformers
{
	/// <summary>
	/// <see cref="ITransformer{TInput, TOutput}"/> for <see cref="GraphQL.Types.User"/>s.
	/// </summary>
	sealed class UserGraphQLTransformer : TransformerBase<User, GraphQL.Types.User>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserGraphQLTransformer"/> class.
		/// </summary>
		public UserGraphQLTransformer()
			: base(
				  BuildSubProjection<
					  UserGroup,
					  PermissionSet,
					  GraphQL.Types.UserGroup,
					  GraphQL.Types.PermissionSet,
					  UserGroupGraphQLTransformer,
					  PermissionSetGraphQLTransformer>(
					  (model, group, ownedPermissionSet) => new GraphQL.Types.User
					  {
						  CreatedAt = model.CreatedAt ?? NotNullFallback<DateTimeOffset>(),
						  CanonicalName = model.CanonicalName ?? NotNullFallback<string>(),
						  CreatedById = model.CreatedById,
						  Enabled = model.Enabled ?? NotNullFallback<bool>(),
						  Id = model.Id!.Value,
						  Name = model.Name ?? NotNullFallback<string>(),
						  SystemIdentifier = model.SystemIdentifier,
						  OwnedPermissionSet = ownedPermissionSet,
						  EffectivePermissionSet = ownedPermissionSet != null
							? ownedPermissionSet
							: group != null
								? group.PermissionSet
								: NotNullFallback<GraphQL.Types.PermissionSet>(),
						  Group = group,
					  },
					  model => model.Group,
					  model => model.PermissionSet))
		{
		}
	}
}
