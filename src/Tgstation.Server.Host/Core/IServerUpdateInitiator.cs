using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Initiates server self updates.
	/// </summary>
	public interface IServerUpdateInitiator
	{
		/// <summary>
		/// Start the process of downloading and applying an update to a new server <paramref name="version"/>.
		/// </summary>
		/// <param name="version">The TGS <see cref="Version"/> to update to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="ServerUpdateResult"/>.</returns>
		Task<ServerUpdateResult> BeginUpdate(Version version, CancellationToken cancellationToken);
	}
}
