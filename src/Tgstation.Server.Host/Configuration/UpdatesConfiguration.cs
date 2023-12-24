using Tgstation.Server.Common;

namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// Configuration for the automatic update system.
	/// </summary>
	public sealed class UpdatesConfiguration
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="UpdatesConfiguration"/> resides in.
		/// </summary>
		public const string Section = "Updates";

		/// <summary>
		/// The tgstation/tgstation-server <see cref="Octokit.Repository.Id"/>.
		/// </summary>
		const long DefaultGitHubRepositoryId = 92952846;

		/// <summary>
		/// The default value of <see cref="GitTagPrefix"/>.
		/// </summary>
		const string DefaultGitTagPrefix = $"{Constants.CanonicalPackageName}-v";

		/// <summary>
		/// The default value of <see cref="UpdatePackageAssetName"/>.
		/// </summary>
		const string DefaultUpdatePackageAssetName = "ServerUpdatePackage.zip";

		/// <summary>
		/// The <see cref="Octokit.Repository.Id"/> of the tgstation-server fork to receive updates from.
		/// </summary>
		public long GitHubRepositoryId { get; set; } = DefaultGitHubRepositoryId;

		/// <summary>
		/// Prefix before the <see cref="global::System.Version"/> of TGS published in git tags.
		/// </summary>
		public string? GitTagPrefix { get; set; } = DefaultGitTagPrefix;

		/// <summary>
		/// Asset package containing the new <see cref="Host"/> assembly in zip form.
		/// </summary>
		public string? UpdatePackageAssetName { get; set; } = DefaultUpdatePackageAssetName;
	}
}
