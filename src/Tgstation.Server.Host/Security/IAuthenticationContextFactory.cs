using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// For creating and accessing authentication contexts
	/// </summary>
	public interface IAuthenticationContextFactory
	{
		/// <summary>
		/// The <see cref="IAuthenticationContext"/> the <see cref="IAuthenticationContextFactory"/> created
		/// </summary>
		IAuthenticationContext CurrentAuthenticationContext { get; }

		/// <summary>
		/// Keep a <paramref name="user"/>'s <paramref name="systemIdentity"/> alive until an <paramref name="expiry"/> time
		/// </summary>
		/// <param name="user">The <see cref="User"/> the <paramref name="systemIdentity"/> belongs to</param>
		/// <param name="systemIdentity">The <see cref="ISystemIdentity"/> to cache</param>
		/// <param name="expiry">When the <paramref name="systemIdentity"/> should expire</param>
		void CacheSystemIdentity(User user, ISystemIdentity systemIdentity, DateTimeOffset expiry);

		/// <summary>
		/// Create an <see cref="IAuthenticationContext"/> to populate <see cref="CurrentAuthenticationContext"/>
		/// </summary>
		/// <param name="userId">The <see cref="Api.Models.Internal.User.Id"/> of the <see cref="IAuthenticationContext.User"/></param>
		/// <param name="instanceId">The <see cref="Api.Models.Instance.Id"/> of the operation</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CreateAuthenticationContext(long userId, long? instanceId, CancellationToken cancellationToken);
	}
}
