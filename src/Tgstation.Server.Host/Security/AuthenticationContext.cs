using System;
using System.Linq;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <inheritdoc />
	sealed class AuthenticationContext : IAuthenticationContext, IDisposable
	{
		/// <inheritdoc />
		public bool Valid { get; private set; }

		/// <inheritdoc />
		public User User => user ?? throw InvalidContext();

		/// <inheritdoc />
		public PermissionSet PermissionSet => permissionSet ?? throw InvalidContext();

		/// <inheritdoc />
		public InstancePermissionSet? InstancePermissionSet { get; private set; }

		/// <inheritdoc />
		public ISystemIdentity? SystemIdentity { get; private set; }

		/// <inheritdoc />
		public DateTimeOffset SessionExpiry => sessionExpiry ?? throw InvalidContext();

		/// <inheritdoc />
		public string SessionId => sessionId ?? throw InvalidContext();

		/// <summary>
		/// Backing field for <see cref="User"/>.
		/// </summary>
		User? user;

		/// <summary>
		/// Backing field for <see cref="PermissionSet"/>.
		/// </summary>
		PermissionSet? permissionSet;

		/// <summary>
		/// Backing field for <see cref="SessionExpiry"/>.
		/// </summary>
		DateTimeOffset? sessionExpiry;

		/// <summary>
		/// Backing field for <see cref="SessionId"/>.
		/// </summary>
		string? sessionId;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationContext"/> class.
		/// </summary>
		public AuthenticationContext()
		{
		}

		/// <summary>
		/// <see cref="InvalidOperationException"/> for accessing fields on an In<see cref="Valid"/> <see cref="AuthenticationContext"/>.
		/// </summary>
		/// <returns>A new <see cref="InvalidOperationException"/>.</returns>
		static InvalidOperationException InvalidContext()
			=> new("AuthenticationContext is invalid!");

		/// <inheritdoc />
		public void Dispose() => SystemIdentity?.Dispose();

		/// <summary>
		/// Initializes the <see cref="AuthenticationContext"/>.
		/// </summary>
		/// <param name="user">The value of <see cref="User"/>.</param>
		/// <param name="sessionExpiry">The value of <see cref="SessionExpiry"/>.</param>
		/// <param name="sessionId">The value of <see cref="SessionId"/>.</param>
		/// <param name="instanceUser">The value of <see cref="InstancePermissionSet"/>.</param>
		/// <param name="systemIdentity">The value of <see cref="SystemIdentity"/>.</param>
		public void Initialize(
			User user,
			DateTimeOffset sessionExpiry,
			string sessionId,
			InstancePermissionSet? instanceUser,
			ISystemIdentity? systemIdentity)
		{
			ArgumentNullException.ThrowIfNull(user);
			ArgumentNullException.ThrowIfNull(sessionId);
			if (systemIdentity == null && user.SystemIdentifier != null)
				throw new ArgumentNullException(nameof(systemIdentity));

			permissionSet = user.PermissionSet
				?? user.Group!.PermissionSet
				?? throw new ArgumentException("No PermissionSet provider", nameof(user));
			this.user = user;
			InstancePermissionSet = instanceUser;
			SystemIdentity = systemIdentity;
			this.sessionId = sessionId;
			this.sessionExpiry = sessionExpiry;

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

			var prop = typeToCheck.GetProperties().Where(x => x.PropertyType == nullableRightsType && x.CanRead).First();

			var right = prop.GetMethod!.Invoke(
				isInstance
					? InstancePermissionSet
					: PermissionSet,
				Array.Empty<object>())
				?? throw new InvalidOperationException("A user right was null!");

			return (ulong)right;
		}
	}
}
