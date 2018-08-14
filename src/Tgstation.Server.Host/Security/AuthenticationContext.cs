using System;
using System.Linq;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Manages <see cref="Api.Models.User"/>s for a scope
	/// </summary>
	sealed class AuthenticationContext : IAuthenticationContext
	{
		/// <inheritdoc />
		public User User
		{
			get
			{
				if (user == null)
					throw new InvalidOperationException("AuthenticationContext has no user!");
				return user;
			}
		}

		/// <inheritdoc />
		public InstanceUser InstanceUser { get; }

		/// <inheritdoc />
		public ISystemIdentity SystemIdentity { get; }

		/// <summary>
		/// Backing field for <see cref="User"/>
		/// </summary>
		readonly User user;

		/// <summary>
		/// Construct an empty <see cref="AuthenticationContext"/>
		/// </summary>
		public AuthenticationContext() { }

		/// <summary>
		/// Construct an <see cref="AuthenticationContext"/>
		/// </summary>
		/// <param name="systemIdentity">The value of <see cref="SystemIdentity"/></param>
		/// <param name="user">The value of <see cref="User"/></param>
		/// <param name="instanceUser">The value of <see cref="InstanceUser"/></param>
		public AuthenticationContext(ISystemIdentity systemIdentity, User user, InstanceUser instanceUser)
		{
			this.user = user ?? throw new ArgumentNullException(nameof(user));
			if (systemIdentity == null && User.SystemIdentifier != null)
				throw new ArgumentNullException(nameof(systemIdentity));
			InstanceUser = instanceUser;
			SystemIdentity = systemIdentity;
		}

		/// <inheritdoc />
		public void Dispose() => SystemIdentity?.Dispose();

		/// <inheritdoc />
		public IAuthenticationContext Clone() => new AuthenticationContext(SystemIdentity.Clone(), User, InstanceUser);

		/// <inheritdoc />
		public ulong GetRight(RightsType rightsType)
		{
			var isInstance = RightsHelper.IsInstanceRight(rightsType);

			//forces the null user check
			var pullThis = User;

			if (isInstance && InstanceUser == null)
				return 0;
			var rightsEnum = RightsHelper.RightToType(rightsType);
			// use the api versions because they're the ones that contain the actual properties
			var typeToCheck = isInstance ? typeof(InstanceUser) : typeof(User);

			var nullableType = typeof(Nullable<>);
			var nullableRightsType = nullableType.MakeGenericType(rightsEnum);

			var prop = typeToCheck.GetProperties().Where(x => x.PropertyType == nullableRightsType).First();

			var right = prop.GetMethod.Invoke(isInstance ? (object)InstanceUser : User, Array.Empty<object>());
			
			if (right == null)
				throw new InvalidOperationException("A user right was null!");
			return (ulong)right;
		}
	}
}
