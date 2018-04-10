using System.Collections.Generic;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a git repository
	/// </summary>
	[Model(RightsType.Repository, ReadRight = RepositoryRights.Read, RequiresInstance = true)]
	public sealed class Repository : Internal.RepositorySettings
	{
		/// <summary>
		/// The commit HEAD points to
		/// </summary>
		[Permissions(WriteRight = RepositoryRights.SetSha)]
		public string NewRevision { get; set; }

		[Permissions(DenyWrite = true)]
		public RevisionInformation RevisionInformation { get; set; }

		/// <summary>
		/// The branch or tag HEAD points to
		/// </summary>
		[Permissions(WriteRight = RepositoryRights.SetReference)]
		public string Reference { get; set; }

		[Permissions(WriteRight = RepositoryRights.MergePullRequest)]
		public List<TestMergeParameters> NewTestMerges { get; set; }
	}
}