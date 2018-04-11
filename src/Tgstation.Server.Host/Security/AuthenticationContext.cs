using System;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Manages <see cref="Api.Models.User"/>s for a scope
	/// </summary>
	sealed class AuthenticationContext : IAuthenticationContext
	{
		/// <inheritdoc />
		public User User { get; }

		/// <inheritdoc />
		public InstanceUser InstanceUser { get; }

		/// <inheritdoc />
		public ISystemIdentity SystemIdentity { get; }

		/// <summary>
		/// Construct a <see cref="IAuthenticationContext"/>
		/// </summary>
		/// <param name="systemIdentity">The value of <see cref="SystemIdentity"/></param>
		/// <param name="user">The value of <see cref="User"/></param>
		/// <param name="instanceUser">The value of <see cref="InstanceUser"/></param>
		public AuthenticationContext(ISystemIdentity systemIdentity, User user, InstanceUser instanceUser)
		{
			User = user ?? throw new ArgumentNullException(nameof(user));
			if (systemIdentity == null && User.SystemIdentifier != null)
				throw new ArgumentNullException(nameof(systemIdentity));
			InstanceUser = instanceUser;
			SystemIdentity = systemIdentity;
		}

		/// <inheritdoc />
		public void Dispose() => SystemIdentity.Dispose();

		/// <inheritdoc />
		public IAuthenticationContext Clone() => new AuthenticationContext(SystemIdentity.Clone(), User, InstanceUser);
	}
}
