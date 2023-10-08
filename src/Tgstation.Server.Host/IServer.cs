using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host
{
	/// <summary>
	/// Represents the host.
	/// </summary>
	public interface IServer
	{
		/// <summary>
		/// If the <see cref="IServer"/> should restart.
		/// </summary>
		bool RestartRequested { get; }

		/// <summary>
		/// Runs the <see cref="IServer"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Run(CancellationToken cancellationToken);
	}
}
