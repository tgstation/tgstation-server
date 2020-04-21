using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <inheritdoc />
	interface IBridgeHandler : IBridgeHandlerBase
	{
		/// <summary>
		/// The <see cref="RuntimeInformation.AccessIdentifier"/> for the <see cref="IBridgeHandler"/>.
		/// </summary>
		string AccessIdentifier { get; }

		/// <summary>
		/// Called when the owning <see cref="Instance"/> is renamed.
		/// </summary>
		/// <param name="newInstanceName">The new <see cref="Api.Models.Instance.Name"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task InstanceRenamed(string newInstanceName, CancellationToken cancellationToken);
	}
}