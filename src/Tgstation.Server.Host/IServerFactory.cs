using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host
{
	/// <summary>
	/// For creating <see cref="IServer"/>s.
	/// </summary>
	public interface IServerFactory
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="IServerFactory"/>.
		/// </summary>
		IIOManager IOManager { get; }

		/// <summary>
		/// Create a <see cref="IServer"/>.
		/// </summary>
		/// <param name="args">The arguments for the <see cref="IServer"/>.</param>
		/// <param name="updatePath">The directory in which to install server updates.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="IServer"/> if it should be run, <see langword="null"/> otherwise.</returns>
		ValueTask<IServer> CreateServer(string[] args, string updatePath, CancellationToken cancellationToken);
	}
}
