using System;
using Octokit;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class GitHubClientFactory : IGitHubClientFactory
	{
		/// <summary>
		/// The <see cref="IApplication"/> for the <see cref="GitHubClientFactory"/>
		/// </summary>
		readonly IApplication application;

		/// <summary>
		/// Construct a <see cref="GitHubClientFactory"/>
		/// </summary>
		/// <param name="application">The value of <see cref="application"/></param>
		public GitHubClientFactory(IApplication application)
		{
			this.application = application ?? throw new ArgumentNullException(nameof(application));
		}

		/// <summary>
		/// Create a <see cref="GitHubClient"/>
		/// </summary>
		/// <returns>A new <see cref="GitHubClient"/></returns>
		GitHubClient CreateBaseClient() => new GitHubClient(new ProductHeaderValue(application.VersionPrefix, application.Version.ToString()));

		/// <inheritdoc />
		public IGitHubClient CreateClient() => CreateBaseClient();

		/// <inheritdoc />
		public IGitHubClient CreateClient(string accessToken)
		{
			if (accessToken == null)
				throw new ArgumentNullException(nameof(accessToken));
			var result = CreateBaseClient();
			result.Credentials = new Credentials(accessToken);
			return result;
		}
	}
}
