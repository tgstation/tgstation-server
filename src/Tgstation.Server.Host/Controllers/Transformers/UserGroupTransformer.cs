using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Controllers.Transformers
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
						  Id = model.Id,
						  Name = model.Name,
						  PermissionSet = permissionSet,
					  },
					  model => model.PermissionSet))
		{
		}
	}
}
