using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Factory for <see cref="IDmbProvider"/>s
	/// </summary>
	public interface IDmbFactory
	{
		/// <summary>
		/// Get a <see cref="Task"/> that completes when the result of a call to <see cref="LockNextDmb(CancellationToken)"/> will be different than the previous call if any
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task OnNewerDmb { get; }

		/// <summary>
		/// Gets the next <see cref="IDmbProvider"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a new <see cref="IDmbProvider"/></returns>
		Task<IDmbProvider> LockNextDmb(CancellationToken cancellationToken);

		/// <summary>
		/// Gets a <see cref="IDmbProvider"/> for a given <see cref="CompileJob"/>
		/// </summary>
		/// <param name="compileJob">The <see cref="CompileJob"/> to make the <see cref="IDmbProvider"/> for</param>
		/// <returns>A new <see cref="IDmbProvider"/></returns>
		IDmbProvider FromCompileJob(CompileJob compileJob);
	}
}
