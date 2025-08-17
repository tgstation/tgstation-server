using System.Linq;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Controllers.Transformers
{
	/// <summary>
	/// <see cref="Models.ITransformer{TInput, TOutput}"/> for <see cref="UserResponse"/>s.
	/// </summary>
	sealed class UserResponseTransformer : Models.TransformerBase<Models.User, UserResponse>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserResponseTransformer"/> class.
		/// </summary>
		public UserResponseTransformer()
			: base(
				  BuildSubProjection<
					  Models.UserGroup,
					  Models.PermissionSet,
					  Models.User,
					  UserGroup,
					  PermissionSet,
					  UserName,
					  UserGroupTransformer,
					  PermissionSetTransformer,
					  UserNameTransformer>(
					  (model, group, permissionSet, createdBy) => new UserResponse
					  {
						  Id = model.Id,
						  CreatedAt = model.CreatedAt,
						  OAuthConnections = model.OAuthConnections != null
							? model.OAuthConnections.Select(
								oAuthConnection => new OAuthConnection
								{
									ExternalUserId = oAuthConnection.ExternalUserId,
									Provider = oAuthConnection.Provider,
								})
								.ToList()
							: null,
						  CreatedBy = createdBy,
						  Enabled = model.Enabled,
						  Group = group,
						  Name = model.Name,
						  OidcConnections = model.OidcConnections != null
							? model.OidcConnections.Select(
								oAuthConnection => new OidcConnection
								{
									ExternalUserId = oAuthConnection.ExternalUserId,
									SchemeKey = oAuthConnection.SchemeKey,
								})
								.ToList()
							: null,
						  PermissionSet = permissionSet,
						  SystemIdentifier = model.SystemIdentifier,
					  },
					  model => model.Group,
					  model => model.PermissionSet,
					  model => model.CreatedBy))
		{
		}
	}
}
