using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Handler for an instance being renamed.
	/// </summary>
	public interface IRenameNotifyee
	{
		/// <summary>
		/// Called when the owning <see cref="Instance"/> is renamed.
		/// </summary>
		/// <param name="newInstanceName">The new <see cref="Api.Models.NamedEntity.Name"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task InstanceRenamed(string newInstanceName, CancellationToken cancellationToken);
	}
}
