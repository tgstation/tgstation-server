using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	/// <inheritdoc />
	interface IBridgeHandler : IBridgeDispatcher
	{
		/// <summary>
		/// The <see cref="DMApiParameters"/> for the <see cref="IBridgeHandler"/>.
		/// </summary>
		DMApiParameters DMApiParameters { get; }

		/// <summary>
		/// Called when the owning <see cref="Instance"/> is renamed.
		/// </summary>
		/// <param name="newInstanceName">The new <see cref="Api.Models.Instance.Name"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task InstanceRenamed(string newInstanceName, CancellationToken cancellationToken);
	}
}