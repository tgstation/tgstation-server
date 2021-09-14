using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class User : Api.Models.Internal.UserModelBase, IApiTransformable<UserResponse>
	{
		/// <summary>
		/// Username used when creating jobs automatically.
		/// </summary>
		public const string TgsSystemUserName = "TGS";

		/// <summary>
		/// <see cref="EntityId.Id"/>.
		/// </summary>
		[NotMapped]
		public new long Id
		{
			get => base.Id ?? throw new InvalidOperationException("Id was null!");
			set => base.Id = value;
		}

		/// <summary>
		/// <see cref="UserName.Name"/>.
		/// </summary>
		[NotMapped]
		public new string Name
		{
			get => base.Name ?? throw new InvalidOperationException("Name was null!");
			set => base.Name = value;
		}

		/// <summary>
		/// The hash of the user's password.
		/// </summary>
		[BackingField(nameof(passwordHash))]
		public string PasswordHash
		{
			get => passwordHash ?? throw new InvalidOperationException("PasswordHash not set!");
			set => passwordHash = value;
		}

		/// <summary>
		/// See <see cref="UserResponse"/>.
		/// </summary>
		public User? CreatedBy { get; set; }

		/// <summary>
		/// The <see cref="UserGroup"/> the <see cref="User"/> belongs to, if any.
		/// </summary>
		public UserGroup? Group { get; set; }

		/// <summary>
		/// The ID of the <see cref="User"/>'s <see cref="UserGroup"/>.
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
		[BackingField(nameof(canonicalName))]
		public string CanonicalName
		{
			get => canonicalName ?? throw new InvalidOperationException("CanonicalName not set!");
			set => canonicalName = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.UserModelBase.Enabled"/>.
		/// </summary>
		[NotMapped]
		public new bool Enabled
		{
			get => base.Enabled ?? throw new InvalidOperationException("Enabled was null!");
			set => base.Enabled = value;
		}

		/// <summary>
		/// When <see cref="PasswordHash"/> was last changed.
		/// </summary>
		public DateTimeOffset? LastPasswordUpdate { get; set; }

		/// <summary>
		/// <see cref="User"/>s created by this <see cref="User"/>.
		/// </summary>
		[BackingField(nameof(createdUsers))]
		public ICollection<User> CreatedUsers
		{
			get => createdUsers ?? throw new InvalidOperationException("CreatedUsers not set!");
			set => createdUsers = value;
		}

		/// <summary>
		/// The <see cref="TestMerge"/>s made by the <see cref="User"/>.
		/// </summary>
		[BackingField(nameof(testMerges))]
		public ICollection<TestMerge> TestMerges
		{
			get => testMerges ?? throw new InvalidOperationException("TestMerges not set!");
			set => testMerges = value;
		}

		/// <summary>
		/// The <see cref="TestMerge"/>s made by the <see cref="User"/>.
		/// </summary>
		[BackingField(nameof(oAuthConnections))]
		public ICollection<OAuthConnection> OAuthConnections
		{
			get => oAuthConnections ?? throw new InvalidOperationException("OAuthConnections not set!");
			set => oAuthConnections = value;
		}

		/// <summary>
		/// Change a <see cref="UserName.Name"/> into a <see cref="CanonicalName"/>.
		/// </summary>
		/// <param name="name">The <see cref="UserName.Name"/>.</param>
		/// <returns>The <see cref="CanonicalName"/>.</returns>
		public static string CanonicalizeName(string name) => name?.ToUpperInvariant() ?? throw new ArgumentNullException(nameof(name));

		/// <summary>
		/// Backing field for <see cref="PasswordHash"/>.
		/// </summary>
		string? passwordHash;

		/// <summary>
		/// Backing field for <see cref="CanonicalName"/>.
		/// </summary>
		string? canonicalName;

		/// <summary>
		/// Backing field for <see cref="CreatedUsers"/>.
		/// </summary>
		ICollection<User>? createdUsers;

		/// <summary>
		/// Backing field for <see cref="TestMerges"/>.
		/// </summary>
		ICollection<TestMerge>? testMerges;

		/// <summary>
		/// Backing field for <see cref="OAuthConnections"/>.
		/// </summary>
		ICollection<OAuthConnection>? oAuthConnections;

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
