using System;
using Microsoft.Extensions.Options;
using Octokit;
using Tgstation.Server.Host.Configuration;
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
		/// The <see cref="GeneralConfiguration"/> for the <see cref="GitHubClientFactory"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// Construct a <see cref="GitHubClientFactory"/>
		/// </summary>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		public GitHubClientFactory(IAssemblyInformationProvider assemblyInformationProvider, IOptions<GeneralConfiguration> generalConfigurationOptions)
		{
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <summary>
		/// Create a <see cref="GitHubClient"/>.
		/// </summary>
		/// <param name="accessToken">Optional access token to use as credentials.</param>
		/// <returns>A new <see cref="GitHubClient"/></returns>
		GitHubClient CreateClientImpl(string accessToken)
		{
			var client = new GitHubClient(
				new ProductHeaderValue(
					assemblyInformationProvider.ProductInfoHeaderValue.Product.Name,
					assemblyInformationProvider.ProductInfoHeaderValue.Product.Version));
			if (!String.IsNullOrWhiteSpace(accessToken))
				client.Credentials = new Credentials(accessToken);

			return client;
		}

		/// <inheritdoc />
		public IGitHubClient CreateClient() => CreateClientImpl(generalConfiguration.GitHubAccessToken);

		/// <inheritdoc />
		public IGitHubClient CreateClient(string accessToken) => CreateClientImpl(accessToken ?? throw new ArgumentNullException(nameof(accessToken)));
	}
}
