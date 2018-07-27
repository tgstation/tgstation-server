namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// Configuration for the automatic update system
	/// </summary>
	public sealed class UpdatesConfiguration
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="UpdatesConfiguration"/> resides in
		/// </summary>
		public const string Section = "Updates";

		/// <summary>
		/// The <see cref="Octokit.Repository.Id"/> of the tgstation-server fork to recieve updates from
		/// </summary>
		public long GitHubRepositoryId { get; set; }

		/// <summary>
		/// Prefix before the <see cref="System.Version"/> of TGS published in git tags
		/// </summary>
		public string GitTagPrefix { get; set; }

		/// <summary>
		/// Asset package containing the new <see cref="Host"/> assembly in zip form
		/// </summary>
		public string UpdatePackageAssetName { get; set; }
	}
}
