using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.IO;
using System.Reflection;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Models.Migrations
{
	/// <summary>
	/// Contains helpers for creating design time <see cref="Models.DatabaseContext{TParentContext}"/>s
	/// </summary>
	static class DesignTimeDbContextFactoryHelpers
	{
		/// <summary>
		/// Path to the json file to use for migrations configuration
		/// </summary>
		const string MigrationsJson = "appsettings.Development.json";

		/// <inheritdoc />
		public static IOptions<DatabaseConfiguration> GetDbContextOptions()
		{
			var builder = new ConfigurationBuilder();
			builder.SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
			builder.AddJsonFile(MigrationsJson);
			var configuration = builder.Build();
			return Options.Create(configuration.GetSection(DatabaseConfiguration.Section).Get<DatabaseConfiguration>());
		}
	}
}
