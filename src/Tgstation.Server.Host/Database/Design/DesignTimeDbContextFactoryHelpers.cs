using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Database.Design
{
	/// <summary>
	/// Contains helpers for creating design time <see cref="DatabaseContext{TParentContext}"/>s
	/// </summary>
	static class DesignTimeDbContextFactoryHelpers
	{
		/// <summary>
		/// Path to the json file to use for migrations configuration
		/// </summary>
		const string RootJson = "appsettings.json";

		/// <summary>
		/// Path to the development json file to use for migrations configuration
		/// </summary>
		const string DevJson = "appsettings.Development.json";

		/// <summary>
		/// Get the <see cref="IOptions{TOptions}"/> for the <see cref="DatabaseConfiguration"/>
		/// </summary>
		/// <returns>The <see cref="IOptions{TOptions}"/> for the <see cref="DatabaseConfiguration"/></returns>
		public static IOptions<DatabaseConfiguration> GetDbContextOptions()
		{
			var builder = new ConfigurationBuilder();
			var assemblyInfoProvider = new AssemblyInformationProvider();
			var ioManager = new DefaultIOManager();
			builder.SetBasePath(ioManager.GetDirectoryName(assemblyInfoProvider.Path));
			builder.AddJsonFile(RootJson);
			builder.AddJsonFile(DevJson);
			var configuration = builder.Build();
			return Options.Create(configuration.GetSection(DatabaseConfiguration.Section).Get<DatabaseConfiguration>());
		}
	}
}
