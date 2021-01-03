using System;
using Octokit;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class GitHubClientFactory : IGitHubClientFactory
	{
		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="GitHubClientFactory"/>
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// Construct a <see cref="GitHubClientFactory"/>
		/// </summary>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/></param>
		public GitHubClientFactory(IAssemblyInformationProvider assemblyInformationProvider)
		{
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
		}

		/// <summary>
		/// Create a <see cref="GitHubClient"/>
		/// </summary>
		/// <returns>A new <see cref="GitHubClient"/></returns>
		GitHubClient CreateBaseClient() => new GitHubClient(
			new ProductHeaderValue(
				assemblyInformationProvider.ProductInfoHeaderValue.Product.Name,
				assemblyInformationProvider.ProductInfoHeaderValue.Product.Version));

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
