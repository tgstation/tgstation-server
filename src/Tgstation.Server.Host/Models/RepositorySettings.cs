using System.ComponentModel.DataAnnotations;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc cref="Api.Models.RepositorySettings" />
	public sealed class RepositorySettings : Api.Models.RepositorySettings, IApiTransformable<RepositoryResponse>
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The instance <see cref="Api.Models.EntityId.Id"/>.
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The parent <see cref="Models.Instance"/>.
		/// </summary>
		[Required]
		public Instance? Instance { get; set; }

		/// <inheritdoc />
		public RepositoryResponse ToApi() => new()
		{
			// AccessToken = AccessToken, // never show this
			AccessUser = AccessUser,
			AutoUpdatesKeepTestMerges = AutoUpdatesKeepTestMerges,
			AutoUpdatesSynchronize = AutoUpdatesSynchronize,
			CommitterEmail = CommitterEmail,
			CommitterName = CommitterName,
			PushTestMergeCommits = PushTestMergeCommits,
			ShowTestMergeCommitters = ShowTestMergeCommitters,
			PostTestMergeComment = PostTestMergeComment,
			CreateGitHubDeployments = CreateGitHubDeployments,
			UpdateSubmodules = UpdateSubmodules,

			// revision information and the rest retrieved by controller
		};
	}
}
