using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Operations that can be performed on a given <see cref="Models.Instance"/>.
	/// </summary>
	public interface IInstanceOperations
	{
		/// <summary>
		/// Online an <see cref="IInstance"/>.
		/// </summary>
		/// <param name="metadata">The <see cref="Models.Instance"/> of the desired <see cref="IInstance"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task OnlineInstance(Models.Instance metadata, CancellationToken cancellationToken);

		/// <summary>
		/// Offline an <see cref="IInstance"/>.
		/// </summary>
		/// <param name="metadata">The <see cref="Models.Instance"/> of the desired <see cref="IInstance"/>.</param>
		/// <param name="user">The <see cref="User"/> performing the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task OfflineInstance(Models.Instance metadata, User user, CancellationToken cancellationToken);

		/// <summary>
		/// Move an <see cref="IInstance"/>.
		/// </summary>
		/// <param name="metadata">The <see cref="Models.Instance"/> of the desired <see cref="IInstance"/> with the updated path.</param>
		/// <param name="oldPath">The old path of the <see cref="IInstance"/>. <paramref name="metadata"/> will have this set on <see cref="Api.Models.Instance.Path"/> if the operation fails.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task MoveInstance(Models.Instance metadata, string oldPath, CancellationToken cancellationToken);
	}
}
