using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class User : Api.Models.Internal.User, IApiTransformable<Api.Models.User>
	{
		/// <summary>
		/// Username used when creating jobs automatically.
		/// </summary>
		public const string TgsSystemUserName = "TGS";

		/// <summary>
		/// The hash of the user's password
		/// </summary>
		public string PasswordHash { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.User"/>
		/// </summary>
		public User CreatedBy { get; set; }

		/// <summary>
		/// The <see cref="UserGroup"/> the <see cref="User"/> belongs to, if any.
		/// </summary>
		public UserGroup Group { get; set; }

		/// <summary>
		/// The ID of the <see cref="User"/>'s <see cref="UserGroup"/>.
		/// </summary>
		public long? GroupId { get; set; }

		/// <summary>
		/// The <see cref="PermissionSet"/> the <see cref="User"/> has, if any.
		/// </summary>
		public PermissionSet PermissionSet { get; set; }

		/// <summary>
		/// The uppercase invariant of <see cref="Api.Models.Internal.UserBase.Name"/>
		/// </summary>
		[Required]
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public string CanonicalName { get; set; }

		/// <summary>
		/// When <see cref="PasswordHash"/> was last changed
		/// </summary>
		public DateTimeOffset? LastPasswordUpdate { get; set; }

		/// <summary>
		/// <see cref="User"/>s created by this <see cref="User"/>
		/// </summary>
		public ICollection<User> CreatedUsers { get; set; }

		/// <summary>
		/// The <see cref="TestMerge"/>s made by the <see cref="User"/>
		/// </summary>
		public ICollection<TestMerge> TestMerges { get; set; }

		/// <summary>
		/// The <see cref="TestMerge"/>s made by the <see cref="User"/>
		/// </summary>
		public ICollection<OAuthConnection> OAuthConnections { get; set; }

		/// <summary>
		/// Change a <see cref="Api.Models.Internal.UserBase.Name"/> into a <see cref="CanonicalName"/>.
		/// </summary>
		/// <param name="name">The <see cref="Api.Models.Internal.UserBase.Name"/>.</param>
		/// <returns>The <see cref="CanonicalName"/>.</returns>
		public static string CanonicalizeName(string name) => name?.ToUpperInvariant() ?? throw new ArgumentNullException(nameof(name));

		/// <summary>
		/// See <see cref="ToApi(bool)"/>
		/// </summary>
		/// <param name="recursive">If we should recurse on <see cref="CreatedBy"/></param>
		/// <param name="showDetails">If rights and system identifier should be shown</param>
		/// <returns>A new <see cref="Api.Models.User"/></returns>
		Api.Models.User ToApi(bool recursive, bool showDetails) => new Api.Models.User
		{
			CreatedAt = showDetails ? CreatedAt : null,
			CreatedBy = showDetails && recursive ? CreatedBy?.ToApi(false, false) : null,
			Enabled = showDetails ? Enabled : null,
			Id = Id,
			Name = Name,
			SystemIdentifier = showDetails ? SystemIdentifier : null,
			OAuthConnections = showDetails
				? OAuthConnections
					?.Select(x => x.ToApi())
					.ToList()
				: null,
			Group = showDetails ? Group?.ToApi(false) : null,
			PermissionSet = showDetails ? PermissionSet?.ToApi() : null,
		};

		/// <summary>
		/// Convert the <see cref="User"/> to it's API form
		/// </summary>
		/// <param name="showDetails">If system identifier, oauth connections, and group/permission set should be shown.</param>
		/// <returns>A new <see cref="Api.Models.User"/></returns>
		public Api.Models.User ToApi(bool showDetails) => ToApi(true, showDetails);

		/// <inheritdoc />
		public Api.Models.User ToApi() => ToApi(true);
	}
}
