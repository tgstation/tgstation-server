using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// Provides features for remote git services
	/// </summary>
	interface IGitRemoteFeatures : IGitRemoteInformation
	{
		/// <summary>
		/// Gets a formatter string which creates the remote refspec for fetching the HEAD of passed in pull request number.
		/// </summary>
		string TestMergeRefSpecFormatter { get; }
	}
}
