using System;
using Tgstation.Server.Api.Rights;
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
		/// The <see cref="User"/>'s effective <see cref="PermissionSet"/>.
		/// </summary>
		PermissionSet PermissionSet { get; }

		/// <summary>
		/// The <see cref="User"/>'s effective <see cref="Models.InstancePermissionSet"/> if applicable.
		/// </summary>
		InstancePermissionSet InstancePermissionSet { get; }

		/// <summary>
		/// Get the value of a given <paramref name="rightsType"/>
		/// </summary>
		/// <param name="rightsType">The <see cref="RightsType"/> of the right to get</param>
		/// <returns>The value of <paramref name="rightsType"/>. Note that if <see cref="InstancePermissionSet"/> is <see langword="null"/> all <see cref="Instance"/> based rights will return 0</returns>
		ulong GetRight(RightsType rightsType);

		/// <summary>
		/// The <see cref="ISystemIdentity"/> of <see cref="User"/> if applicable
		/// </summary>
		ISystemIdentity SystemIdentity { get; }
	}
}
