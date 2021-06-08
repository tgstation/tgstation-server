using System;
using System.Diagnostics;
using System.Globalization;

using Elastic.CommonSchema.Serilog;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;
using Serilog.Sinks.Elasticsearch;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extensions for <see cref="IServiceCollection"/>.
	/// </summary>
	static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Common template used for adding our custom log context to serilog.
		/// </summary>
		public const string SerilogContextTemplate = "(Instance:{Instance}|Job:{Job}|Request:{Request}|User:{User}|Monitor:{Monitor}|Bridge:{Bridge}|Chat:{ChatMessage}";

		/// <summary>
		/// Add a standard <typeparamref name="TConfig"/> binding.
		/// </summary>
		/// <typeparam name="TConfig">The <see langword="class"/> to bind. Must have a <see langword="public"/> const/static <see cref="string"/> field named "Section".</typeparam>
		/// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
		/// <param name="configuration">The <see cref="IConfiguration"/> containing the <typeparamref name="TConfig"/>.</param>
		/// <returns><paramref name="serviceCollection"/>.</returns>
		public static IServiceCollection UseStandardConfig<TConfig>(this IServiceCollection serviceCollection, IConfiguration configuration) where TConfig : class
		{
			if (serviceCollection == null)
				throw new ArgumentNullException(nameof(serviceCollection));
			if (configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			const string SectionFieldName = nameof(GeneralConfiguration.Section);

			var configType = typeof(TConfig);
			var sectionField = configType.GetField(SectionFieldName);
			if (sectionField == null)
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "{0} has no {1} field!", configType, SectionFieldName));

			var stringType = typeof(string);
			if (sectionField.FieldType != stringType)
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "{0} has invalid {1} field type, must be {2}!", configType, SectionFieldName, stringType));

			var sectionName = (string)sectionField.GetValue(null);

			return serviceCollection.Configure<TConfig>(configuration.GetSection(sectionName));
		}

		/// <summary>
		/// Clear previous providers and configure logging.
		/// </summary>
		/// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
		/// <param name="configurationAction">Additional configuration for a given <see cref="LoggerConfiguration"/>.</param>
		/// <param name="tgsConfiguration">The service configuration for a given <see cref="IConfiguration"/>.</param>
		/// <param name="sinkConfigurationAction">Additional configuration for a given <see cref="LoggerSinkConfiguration"/>.</param>
		/// <returns>The updated <paramref name="serviceCollection"/>.</returns>
		public static IServiceCollection SetupLogging(
			this IServiceCollection serviceCollection,
			Action<LoggerConfiguration> configurationAction,
			IConfiguration tgsConfiguration,
			Action<LoggerSinkConfiguration> sinkConfigurationAction = null)
			=> serviceCollection.AddLogging(builder =>
			{
				builder.ClearProviders();

				var configuration = new LoggerConfiguration()
					.MinimumLevel
						.Verbose();

				configurationAction?.Invoke(configuration);

				configuration
					.Enrich.FromLogContext()
					.WriteTo
					.Async(sinkConfiguration =>
					{
						sinkConfiguration.Console(
							outputTemplate: "[{Timestamp:HH:mm:ss}] {Level:w3}: {SourceContext:l} "
								+ SerilogContextTemplate
								+ "|IR:{InstanceReference}){NewLine}    {Message:lj}{NewLine}{Exception}");
						sinkConfigurationAction?.Invoke(sinkConfiguration);
					});

				string elasticsearchEndpoint = tgsConfiguration.GetSection("Elasticsearch").GetSection("Host").Value;
				string elasticsearchUser = tgsConfiguration.GetSection("Elasticsearch").GetSection("Username").Value;
				string elasticsearchPassword = tgsConfiguration.GetSection("Elasticsearch").GetSection("Password").Value;
				string raw_bool = tgsConfiguration.GetSection("Elasticsearch").GetSection("Enable").Value;
				bool elasticsearchEnabled = false;

				if (!string.IsNullOrEmpty(raw_bool))
					elasticsearchEnabled = bool.Parse(raw_bool);

				if (elasticsearchEnabled)
				{
					if (elasticsearchEndpoint == null)
						throw new InvalidOperationException("Elasticsearch endpoint is null!");

					configuration.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticsearchEndpoint))
					{
						ModifyConnectionSettings = x => (!string.IsNullOrEmpty(elasticsearchUser) && !string.IsNullOrEmpty(elasticsearchPassword)) ? x.BasicAuthentication(elasticsearchUser, elasticsearchPassword) : null,
						CustomFormatter = new EcsTextFormatter(),
						AutoRegisterTemplate = true,
						AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
						IndexFormat = "tgs4-logs",
					});
				}

				builder.AddSerilog(configuration.CreateLogger(), true);

				if (Debugger.IsAttached)
					builder.AddDebug();
			});
	}
}
