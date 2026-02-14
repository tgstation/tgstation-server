using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// Provides features for remote git services.
	/// </summary>
	public interface IGitRemoteFeatures : IGitRemoteAdditionalInformation
	{
		/// <summary>
		/// Gets the remote refspec for fetching the HEAD of passed in test merge.
		/// </summary>
		/// <param name="parameters">The <see cref="TestMergeParameters"/>.</param>
		/// <returns>The refspec.</returns>
		string GetTestMergeRefSpec(TestMergeParameters parameters);

		/// <summary>
		/// Gets the local branch name for the passed in test merge.
		/// </summary>
		/// <param name="parameters">The <see cref="TestMergeParameters"/>.</param>
		/// <returns>The local branch name.</returns>
		string GetTestMergeLocalBranchName(TestMergeParameters parameters);

		/// <summary>
		/// Transform a service's <paramref name="rawPassword"/> into a password usable by git.
		/// </summary>
		/// <param name="rawPassword">The raw password to transform.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the transformed password.</returns>
		public ValueTask<string?> TransformRepositoryPassword(string? rawPassword, CancellationToken cancellationToken);

		/// <summary>
		/// Get the remote URL for a given <paramref name="owner"/> and <paramref name="name"/>.
		/// </summary>
		/// <param name="owner">The repository owner.</param>
		/// <param name="name">The repository name.</param>
		/// <returns>The remote repository <see cref="Uri"/>.</returns>
		Uri GetRemoteUrl(string owner, string name);

		/// <summary>
		/// Get the repository owner and name for the given <paramref name="parameters"/>.
		/// </summary>
		/// <param name="parameters">The <see cref="TestMergeParameters"/>.</param>
		/// <returns>A tuple containing the owner and name.</returns>
		(string Owner, string Name) GetRepositoryOwnerAndName(TestMergeParameters parameters);
	}
}
