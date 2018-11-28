using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// The command line <see cref="Configuration"/> setup wizard
	/// </summary>
	interface ISetupWizard
	{
		/// <summary>
		/// Run the setup wizard if necessary
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the wizard ran, <see langword="false"/> otherwise</returns>
		Task<bool> CheckRunWizard(CancellationToken cancellationToken);
	}
}
