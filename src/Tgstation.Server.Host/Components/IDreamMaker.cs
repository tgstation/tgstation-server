using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For managing the compiler
	/// </summary>
	interface IDreamMaker : IHostedService
	{
		/// <summary>
		/// Starts a compile
		/// </summary>
		/// <param name="dmePath">The .dme file to use</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the partially populated <see cref="CompileJob"/> for the operation</returns>
		Task<CompileJob> Compile(string dmePath, CancellationToken cancellationToken);
	}
}