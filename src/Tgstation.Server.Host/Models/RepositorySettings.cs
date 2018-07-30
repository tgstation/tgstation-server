using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class RepositorySettings : Api.Models.Internal.RepositorySettings, IApiConvertable<Repository>
	{
		/// <summary>
		/// The row Id
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.Instance.Id"/>
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The parent <see cref="Models.Instance"/>
		/// </summary>
		[Required]
		public Instance Instance { get; set; }

		/// <inheritdoc />
		public Repository ToApi() => new Repository {
			AccessToken = AccessToken,
			AccessUser = AccessUser,
			AutoUpdatesKeepTestMerges = AutoUpdatesKeepTestMerges,
			AutoUpdatesSynchronize = AutoUpdatesSynchronize,
			CommitterEmail = CommitterEmail,
			CommitterName = CommitterName,
			//intentionally don't populate origin just in case
			PushTestMergeCommits = PushTestMergeCommits,
			//revision information and the rest retrieved by controller
			ShowTestMergeCommitters = ShowTestMergeCommitters
		};
	}
}
