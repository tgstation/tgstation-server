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
		/// <param name="dmeName">The .dme file to use without the extension</param>
		/// <param name="repository">The <see cref="IRepository"/> to copy from</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the partially populated <see cref="CompileJob"/> for the operation. In particular, note the <see cref="CompileJob.RevisionInformation"/> field will only have it's <see cref="Api.Models.Internal.RevisionInformation.Commit"/> field populated</returns>
		Task<CompileJob> Compile(string dmeName, IRepository repository, CancellationToken cancellationToken);
	}
}