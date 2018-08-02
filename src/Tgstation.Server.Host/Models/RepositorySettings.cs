using System;
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

		/// <summary>
		/// Generates a <see cref="Components.Repository.IRepository"/> access <see cref="string"/>
		/// </summary>
		/// <returns>The access <see cref="string"/> for the <see cref="RepositorySettings"/></returns>
		public string GetAccessString() => AccessUser != null ? String.Concat(AccessUser, '@', AccessToken) : null;

		/// <inheritdoc />
		public Repository ToApi() => new Repository {
			//AccessToken = AccessToken,	//never show this
			AccessUser = AccessUser,
			AutoUpdatesKeepTestMerges = AutoUpdatesKeepTestMerges,
			AutoUpdatesSynchronize = AutoUpdatesSynchronize,
			CommitterEmail = CommitterEmail,
			CommitterName = CommitterName,
			PushTestMergeCommits = PushTestMergeCommits,
			ShowTestMergeCommitters = ShowTestMergeCommitters
			//revision information and the rest retrieved by controller
		};
	}
}
