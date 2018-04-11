using System;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Represents the currently authenticated <see cref="Api.Models.User"/>
	/// </summary>
	public interface IAuthenticationContext : IDisposable
	{
		/// <summary>
		/// The authenticated user
		/// </summary>
		User User { get; }

		/// <summary>
		/// The <see cref="InstanceUser"/> of <see cref="User"/> if applicable
		/// </summary>
		InstanceUser InstanceUser { get; }

		/// <summary>
		/// The <see cref="ISystemIdentity"/> of <see cref="User"/> if applicable
		/// </summary>
		ISystemIdentity SystemIdentity { get; }

		/// <summary>
		/// Creates a copy of the <see cref="IAuthenticationContext"/>
		/// </summary>
		/// <returns>A new <see cref="IAuthenticationContext"/></returns>
		IAuthenticationContext Clone();
	}
}