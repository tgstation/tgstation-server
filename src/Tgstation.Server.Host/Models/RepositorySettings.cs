using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class RepositorySettings : Api.Models.Internal.RepositorySettings, IApiTransformable<Repository>
	{
		/// <summary>
		/// The row Id
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The instance <see cref="EntityId.Id"/>
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The parent <see cref="Models.Instance"/>
		/// </summary>
		[Required]
		public Instance Instance { get; set; }

		/// <inheritdoc />
		public Repository ToApi() => new Repository
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

			// revision information and the rest retrieved by controller
		};
	}
}
