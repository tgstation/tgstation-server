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
	}
}
