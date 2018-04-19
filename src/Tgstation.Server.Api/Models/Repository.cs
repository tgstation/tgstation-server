using System.Collections.Generic;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a git repository
	/// </summary>
	public sealed class Repository : Internal.RepositorySettings
	{
		/// <summary>
		/// The origin URL. If <see langword="null"/>, the <see cref="Repository"/> does not exist
		/// </summary>
		[Permissions(WriteRight = RepositoryRights.SetOrigin)]
		public string Origin { get; set; }

		/// <summary>
		/// The commit HEAD points to
		/// </summary>
		[Permissions(WriteRight = RepositoryRights.SetSha)]
		public string NewRevision { get; set; }

		/// <summary>
		/// The current <see cref="Models.RevisionInformation"/> for the <see cref="Repository"/>
		/// </summary>
		[Permissions(DenyWrite = true)]
		public RevisionInformation RevisionInformation { get; set; }

		/// <summary>
		/// If the repository was cloned from GitHub.com. If <see langword="true"/> this enables test merge functionality
		/// </summary>
		[Permissions(DenyWrite = true)]
		public bool IsGitHub { get; set; }

		/// <summary>
		/// The branch or tag HEAD points to
		/// </summary>
		[Permissions(WriteRight = RepositoryRights.SetReference)]
		public string Reference { get; set; }

		/// <summary>
		/// <see cref="TestMergeParameters"/> for new <see cref="TestMerge"/>s
		/// </summary>
		[Permissions(WriteRight = RepositoryRights.MergePullRequest)]
		public List<TestMergeParameters> NewTestMerges { get; set; }
	}
}