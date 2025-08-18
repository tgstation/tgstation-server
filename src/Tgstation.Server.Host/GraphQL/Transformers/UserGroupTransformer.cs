using Tgstation.Server.Host.GraphQL.Types;

namespace Tgstation.Server.Host.GraphQL.Transformers
{
	/// <summary>
	/// <see cref="Models.ITransformer{TInput, TOutput}"/> for <see cref="UserGroup"/>s.
	/// </summary>
	sealed class UserGroupTransformer : Models.TransformerBase<Models.UserGroup, UserGroup>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserGroupTransformer"/> class.
		/// </summary>
		public UserGroupTransformer()
			: base(
				  BuildSubProjection<Models.PermissionSet, PermissionSet, PermissionSetTransformer>(
					  (model, permissionSet) => new UserGroup
					  {
						  Id = model.Id!.Value,
						  Name = model.Name!,
						  PermissionSet = permissionSet!,
					  },
					  model => model.PermissionSet))
		{
		}
	}
}
