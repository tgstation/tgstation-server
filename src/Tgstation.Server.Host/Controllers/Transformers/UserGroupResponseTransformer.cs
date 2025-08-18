using System.Collections.Generic;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Controllers.Transformers
{
	/// <summary>
	/// <see cref="Models.ITransformer{TInput, TOutput}"/> for <see cref="Models.UserGroup"/>s to <see cref="UserGroupResponse"/>s.
	/// </summary>
	sealed class UserGroupResponseTransformer : Models.TransformerBase<Models.UserGroup, UserGroupResponse>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserGroupResponseTransformer"/> class.
		/// </summary>
		public UserGroupResponseTransformer()
			: base(
				  BuildSubProjection<
					  Models.PermissionSet,
					  IEnumerable<Models.User>,
					  PermissionSet,
					  List<UserName>,
					  PermissionSetTransformer,
					  Models.CollectionTransformer<Models.User, UserName, UserNameTransformer>>(
					  (model, permissionSet, users) => new UserGroupResponse
					  {
						  Id = model.Id,
						  Name = model.Name,
						  PermissionSet = permissionSet,
						  Users = users,
					  },
					  model => model.PermissionSet,
					  model => model.Users))
		{
		}
	}
}
