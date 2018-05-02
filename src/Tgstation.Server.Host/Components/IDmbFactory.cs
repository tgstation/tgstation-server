using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Factory for <see cref="IDmbProvider"/>s
	/// </summary>
    interface IDmbFactory
    {
		/// <summary>
		/// Gets the next <see cref="IDmbProvider"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task<IDmbProvider> LockNextDmb(CancellationToken cancellationToken);

		/// <summary>
		/// Get a <see cref="Task"/> that completes when the result of a call to <see cref="LockNextDmb(CancellationToken)"/> will be different than the previous call if any
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task OnNewerDmb();
    }
}
