using System;
using System.Threading;
using System.Threading.Tasks;

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
		/// Create an <see cref="IAuthenticationContext"/> to populate <see cref="CurrentAuthenticationContext"/>
		/// </summary>
		/// <param name="userId">The <see cref="Api.Models.Internal.User.Id"/> of the <see cref="IAuthenticationContext.User"/></param>
		/// <param name="instanceId">The <see cref="Api.Models.EntityId.Id"/> of the operation</param>
		/// <param name="validAfter">The <see cref="DateTimeOffset"/> the resulting <see cref="IAuthenticationContext.User"/>'s password must be valid after</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CreateAuthenticationContext(long userId, long? instanceId, DateTimeOffset validAfter, CancellationToken cancellationToken);
	}
}
