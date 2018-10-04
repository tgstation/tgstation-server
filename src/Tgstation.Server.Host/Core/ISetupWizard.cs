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
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		Task<bool> CheckRunWizard(CancellationToken cancellationToken);
	}
}
