using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// Sink for <see cref="CompileJob"/>s
	/// </summary>
	public interface ICompileJobSink : ILatestCompileJobProvider
	{
		/// <summary>
		/// Load a new <paramref name="job"/> into the <see cref="ICompileJobSink"/>
		/// </summary>
		/// <param name="job">The <see cref="CompileJob"/> to load</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task LoadCompileJob(CompileJob job, CancellationToken cancellationToken);
	}
}