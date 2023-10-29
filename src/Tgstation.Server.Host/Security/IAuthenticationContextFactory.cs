using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// For creating and accessing authentication contexts.
	/// </summary>
	public interface IAuthenticationContextFactory
	{
		/// <summary>
		/// Create an <see cref="IAuthenticationContext"/> in the request pipeline for a given <paramref name="userId"/> and <paramref name="instanceId"/>.
		/// </summary>
		/// <param name="userId">The <see cref="Api.Models.EntityId.Id"/> of the <see cref="Models.User"/>.</param>
		/// <param name="instanceId">The <see cref="Api.Models.EntityId.Id"/> of the <see cref="Models.Instance"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the created <see cref="IAuthenticationContext"/>.</returns>
		ValueTask<IAuthenticationContext> CreateAuthenticationContext(long userId, long? instanceId, CancellationToken cancellationToken);
	}
}
