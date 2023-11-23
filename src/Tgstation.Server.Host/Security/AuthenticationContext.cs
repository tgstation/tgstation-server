﻿using System;
using System.Linq;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Models;

#nullable disable

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class AuthenticationContext : IAuthenticationContext, IDisposable
	{
		/// <inheritdoc />
		public bool Valid { get; private set; }

		/// <inheritdoc />
		public User User { get; private set; }

		/// <inheritdoc />
		public PermissionSet PermissionSet { get; private set; }

		/// <inheritdoc />
		public InstancePermissionSet InstancePermissionSet { get; private set; }

		/// <inheritdoc />
		public ISystemIdentity SystemIdentity { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationContext"/> class.
		/// </summary>
		public AuthenticationContext()
		{
		}

		/// <inheritdoc />
		public void Dispose() => SystemIdentity?.Dispose();

		/// <summary>
		/// Initializes the <see cref="AuthenticationContext"/>.
		/// </summary>
		/// <param name="systemIdentity">The value of <see cref="SystemIdentity"/>.</param>
		/// <param name="user">The value of <see cref="User"/>.</param>
		/// <param name="instanceUser">The value of <see cref="InstancePermissionSet"/>.</param>
		public void Initialize(ISystemIdentity systemIdentity, User user, InstancePermissionSet instanceUser)
		{
			User = user ?? throw new ArgumentNullException(nameof(user));
			if (systemIdentity == null && User.SystemIdentifier != null)
				throw new ArgumentNullException(nameof(systemIdentity));
			PermissionSet = user.PermissionSet
				?? user.Group.PermissionSet
				?? throw new ArgumentException("No PermissionSet provider", nameof(user));
			InstancePermissionSet = instanceUser;
			SystemIdentity = systemIdentity;

			Valid = true;
		}

		/// <inheritdoc />
		public ulong GetRight(RightsType rightsType)
		{
			var isInstance = RightsHelper.IsInstanceRight(rightsType);

			if (User == null)
				throw new InvalidOperationException("Authentication context has no user!");

			if (isInstance && InstancePermissionSet == null)
				return 0;
			var rightsEnum = RightsHelper.RightToType(rightsType);

			// use the api versions because they're the ones that contain the actual properties
			var typeToCheck = isInstance ? typeof(InstancePermissionSet) : typeof(PermissionSet);

			var nullableType = typeof(Nullable<>);
			var nullableRightsType = nullableType.MakeGenericType(rightsEnum);

			var prop = typeToCheck.GetProperties().Where(x => x.PropertyType == nullableRightsType).First();

			var right = prop.GetMethod.Invoke(
				isInstance
					? InstancePermissionSet
					: PermissionSet,
				Array.Empty<object>());

			if (right == null)
				throw new InvalidOperationException("A user right was null!");

			return (ulong)right;
		}
	}
}
