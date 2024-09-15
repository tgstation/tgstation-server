using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Models.Transformers;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc cref="Api.Models.Internal.UserModelBase" />
	public sealed class User : Api.Models.Internal.UserModelBase,
		ILegacyApiTransformable<UserResponse>,
		IApiTransformable<User, GraphQL.Types.User, UserGraphQLTransformer>,
		IApiTransformable<User, GraphQL.Types.UserName, UserNameGraphQLTransformer>
	{
		/// <summary>
		/// Username used when creating jobs automatically.
		/// </summary>
		public const string TgsSystemUserName = "TGS";

		/// <summary>
		/// The hash of the user's password.
		/// </summary>
		public string? PasswordHash { get; set; }

		/// <summary>
		/// See <see cref="UserResponse"/>.
		/// </summary>
		public User? CreatedBy { get; set; }

		/// <summary>
		/// The <see cref="EntityId.Id"/> of the <see cref="User"/>'s <see cref="CreatedBy"/> <see cref="User"/>.
		/// </summary>
		public long? CreatedById { get; set; }

		/// <summary>
		/// The <see cref="UserGroup"/> the <see cref="User"/> belongs to, if any.
		/// </summary>
		public UserGroup? Group { get; set; }

		/// <summary>
		/// The <see cref="EntityId.Id"/> of the <see cref="User"/>'s <see cref="Group"/>.
		/// </summary>
		public long? GroupId { get; set; }

		/// <summary>
		/// The <see cref="PermissionSet"/> the <see cref="User"/> has, if any.
		/// </summary>
		public PermissionSet? PermissionSet { get; set; }

		/// <summary>
		/// The uppercase invariant of <see cref="UserName.Name"/>.
		/// </summary>
		[Required]
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public string? CanonicalName { get; set; }

		/// <summary>
		/// When <see cref="PasswordHash"/> was last changed.
		/// </summary>
		public DateTimeOffset? LastPasswordUpdate { get; set; }

		/// <summary>
		/// <see cref="User"/>s created by this <see cref="User"/>.
		/// </summary>
		public ICollection<User>? CreatedUsers { get; set; }

		/// <summary>
		/// The <see cref="TestMerge"/>s made by the <see cref="User"/>.
		/// </summary>
		public ICollection<TestMerge>? TestMerges { get; set; }

		/// <summary>
		/// The <see cref="TestMerge"/>s made by the <see cref="User"/>.
		/// </summary>
		public ICollection<OAuthConnection>? OAuthConnections { get; set; }

		/// <summary>
		/// Change a <see cref="UserName.Name"/> into a <see cref="CanonicalName"/>.
		/// </summary>
		/// <param name="name">The <see cref="UserName.Name"/>.</param>
		/// <returns>The <see cref="CanonicalName"/>.</returns>
		public static string CanonicalizeName(string name) => name?.ToUpperInvariant() ?? throw new ArgumentNullException(nameof(name));

		/// <inheritdoc />
		public UserResponse ToApi() => CreateUserResponse(true);

		/// <summary>
		/// Generate a <see cref="UserResponse"/> from <see langword="this"/>.
		/// </summary>
		/// <param name="recursive">If we should recurse on <see cref="CreatedBy"/>.</param>
		/// <returns>A new <see cref="UserResponse"/>.</returns>
		UserResponse CreateUserResponse(bool recursive)
		{
			var result = CreateUserName<UserResponse>();
			if (recursive)
				result.CreatedBy = CreatedBy?.CreateUserName<UserName>();

			result.CreatedAt = CreatedAt;
			result.Enabled = Enabled;
			result.SystemIdentifier = SystemIdentifier;
			result.OAuthConnections = OAuthConnections
				?.Select(x => x.ToApi())
				.ToList();
			result.Group = Group?.ToApi(false);
			result.PermissionSet = PermissionSet?.ToApi();
			return result;
		}
	}
}
