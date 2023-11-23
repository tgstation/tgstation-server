using System.Threading;
using System.Threading.Tasks;

#nullable disable

namespace Tgstation.Server.Host.Utils
{
	/// <summary>
	/// Gets unassigned ports for use by TGS.
	/// </summary>
	public interface IPortAllocator
	{
		/// <summary>
		/// Gets a port not currently in use by TGS.
		/// </summary>
		/// <param name="basePort">The port to check first. Will not allocate a port lower than this.</param>
		/// <param name="checkOne">If only <paramref name="basePort"/> should be checked and no others.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the first available port on success, <see langword="null"/> on failure.</returns>
		ValueTask<ushort?> GetAvailablePort(ushort basePort, bool checkOne, CancellationToken cancellationToken);
	}
}
