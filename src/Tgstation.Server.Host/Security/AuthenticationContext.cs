using System;
using System.Linq;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class AuthenticationContext : IAuthenticationContext
	{
		/// <inheritdoc />
		public bool Valid => user != null;

		/// <inheritdoc />
		public User User => user ?? throw new InvalidOperationException("AuthenticationContext not valid!");

		/// <inheritdoc />
		public PermissionSet PermissionSet => permissionSet ?? throw new InvalidOperationException("AuthenticationContext not valid!");

		/// <inheritdoc />
		public InstancePermissionSet InstancePermissionSet => instancePermissionSet ?? throw new InvalidOperationException("AuthenticationContext has no InstancePermissionSet!");

		/// <inheritdoc />
		public ISystemIdentity? SystemIdentity { get; }

		/// <summary>
		/// Backing field for <see cref="User"/>.
		/// </summary>
		readonly User? user;

		/// <summary>
		/// Backing field for <see cref="PermissionSet"/>.
		/// </summary>
		readonly PermissionSet? permissionSet;

		/// <summary>
		/// Backing field for <see cref="InstancePermissionSet"/>.
		/// </summary>
		readonly InstancePermissionSet? instancePermissionSet;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationContext"/> class.
		/// </summary>
		public AuthenticationContext()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationContext"/> class.
		/// </summary>
		/// <param name="systemIdentity">The value of <see cref="SystemIdentity"/>.</param>
		/// <param name="user">The value of <see cref="User"/>.</param>
		/// <param name="instancePermissionSet">The value of <see cref="InstancePermissionSet"/>.</param>
		public AuthenticationContext(ISystemIdentity? systemIdentity, User user, InstancePermissionSet? instancePermissionSet)
		{
			this.user = user ?? throw new ArgumentNullException(nameof(user));
			if (systemIdentity == null && User.SystemIdentifier != null)
				throw new ArgumentNullException(nameof(systemIdentity));
			permissionSet = user.PermissionSet
				?? user.Group?.PermissionSet
				?? throw new ArgumentException("No PermissionSet provider", nameof(user));
			this.instancePermissionSet = instancePermissionSet;
			SystemIdentity = systemIdentity;
		}

		/// <inheritdoc />
		public void Dispose() => SystemIdentity?.Dispose();

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

			var right = prop.GetMethod!.Invoke(isInstance ? InstancePermissionSet : PermissionSet, Array.Empty<object>());

			if (right == null)
				throw new InvalidOperationException("A user right was null!");
			return (ulong)right;
		}
	}
}
