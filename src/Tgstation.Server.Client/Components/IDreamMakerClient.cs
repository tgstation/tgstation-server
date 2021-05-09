using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing the compiler.
	/// </summary>
	public interface IDreamMakerClient
	{
		/// <summary>
		/// Get the <see cref="DreamMakerResponse"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="DreamMakerResponse"/>.</returns>
		Task<DreamMakerResponse> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Updates the <see cref="Api.Models.Internal.DreamMakerSettings"/>.
		/// </summary>
		/// <param name="dreamMaker">The <see cref="DreamMakerRequest"/> to update.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task<DreamMakerResponse> Update(DreamMakerRequest dreamMaker, CancellationToken cancellationToken);

		/// <summary>
		/// Compile the current repository revision.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="JobResponse"/> for the compile.</returns>
		Task<JobResponse> Compile(CancellationToken cancellationToken);

		/// <summary>
		/// Gets the <see cref="CompileJobResponse"/>s for the instance.
		/// </summary>
		/// <param name="paginationSettings">The optional <see cref="PaginationSettings"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of <see cref="CompileJobResponse"/>s.</returns>
		Task<IReadOnlyList<CompileJobResponse>> ListCompileJobs(PaginationSettings? paginationSettings, CancellationToken cancellationToken);

		/// <summary>
		/// Get a <paramref name="compileJob"/>.
		/// </summary>
		/// <param name="compileJob">The <see cref="Api.Models.Internal.CompileJob"/>'s <see cref="EntityId"/> to get.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="CompileJobResponse"/>.</returns>
		Task<CompileJobResponse> GetCompileJob(EntityId compileJob, CancellationToken cancellationToken);
	}
}
