using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// Additional information from git remotes.
	/// </summary>
	public interface IGitRemoteAdditionalInformation
	{
		/// <summary>
		/// The <see cref="Api.Models.GitRemoteInformation"/> if any.
		/// </summary>
		GitRemoteInformation? GitRemoteInformation { get; }

		/// <summary>
		/// Retrieve the <see cref="Models.TestMerge"/> representation of given test merge <paramref name="parameters"/>.
		/// </summary>
		/// <param name="parameters">The <see cref="TestMergeParameters"/>.</param>
		/// <param name="repositorySettings">The <see cref="RepositorySettings"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Models.TestMerge"/> of the <paramref name="parameters"/>.</returns>
		Task<Models.TestMerge> GetTestMerge(
			TestMergeParameters parameters,
			RepositorySettings repositorySettings,
			CancellationToken cancellationToken);
	}
}
