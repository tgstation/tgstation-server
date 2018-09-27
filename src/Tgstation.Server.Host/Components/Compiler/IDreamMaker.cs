using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Repository;

namespace Tgstation.Server.Host.Components.Compiler
{
	/// <summary>
	/// For managing the compiler
	/// </summary>
	public interface IDreamMaker
	{
		/// <summary>
		/// Starts a compile
		/// </summary>
		/// <param name="revisionInformation">The <see cref="Models.RevisionInformation"/> being compiled from the <paramref name="repository"/></param>
		/// <param name="dreamMakerSettings">The <see cref="Api.Models.DreamMaker"/> for the compile</param>
		/// <param name="apiValidateTimeout">The time in seconds to wait while validating the API</param>
		/// <param name="repository">The <see cref="IRepository"/> to copy from</param>
		/// <param name="progressReporter">The <see cref="Action{T1}"/> to report compilation progress</param>
		/// <param name="estimatedDuration">The estimated amount of time the compile will take</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the partially populated <see cref="Models.CompileJob"/> for the operation. In particular, note the <see cref="Models.CompileJob.RevisionInformation"/> field will only have it's <see cref="Api.Models.Internal.RevisionInformation.CommitSha"/> field populated</returns>
		Task<Models.CompileJob> Compile(Models.RevisionInformation revisionInformation, Api.Models.DreamMaker dreamMakerSettings, uint apiValidateTimeout, IRepository repository, Action<int> progressReporter, TimeSpan? estimatedDuration, CancellationToken cancellationToken);
	}
}