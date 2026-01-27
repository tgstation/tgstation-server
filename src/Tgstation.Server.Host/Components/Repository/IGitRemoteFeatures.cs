using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// Provides features for remote git services.
	/// </summary>
	public interface IGitRemoteFeatures : IGitRemoteAdditionalInformation
	{
		/// <summary>
		/// Gets a formatter string which creates the remote refspec for fetching the HEAD of passed in test merge number.
		/// </summary>
		string TestMergeRefSpecFormatter { get; }

		/// <summary>
		/// Get.
		/// </summary>
		string TestMergeLocalBranchNameFormatter { get; }

		/// <summary>
		/// Transform a service's <paramref name="rawPassword"/> into a password usable by git.
		/// </summary>
		/// <param name="rawPassword">The raw password to transform.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the transformed password.</returns>
		public ValueTask<string?> TransformRepositoryPassword(string? rawPassword, CancellationToken cancellationToken);
	}
}
