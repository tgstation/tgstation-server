using System;
using System.Diagnostics;
using System.Globalization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Serilog;
using Serilog.Configuration;
using Serilog.Sinks.Elasticsearch;

using Tgstation.Server.Host.Components.Chat.Providers;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Utils;
using Tgstation.Server.Host.Utils.GitHub;
using Tgstation.Server.Host.Utils.SignalR;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extensions for <see cref="IServiceCollection"/>.
	/// </summary>
	static class ServiceCollectionExtensions
	{
		/// <summary>
		/// The <see cref="IProviderFactory"/> implementation used in calls to <see cref="AddChatProviderFactory(IServiceCollection)"/>.
		/// </summary>
		static Type? chatProviderFactoryType;

		/// <summary>
		/// The <see cref="IGitHubServiceFactory"/> implementation used in calls to <see cref="AddGitHub(IServiceCollection)"/>.
		/// </summary>
		static Type? gitHubServiceFactoryType;

		/// <summary>
		/// The <see cref="IFileDownloader"/> implementation used in calls to <see cref="AddFileDownloader(IServiceCollection)"/>.
		/// </summary>
		static Type? fileDownloaderType;

		/// <summary>
		/// A <see cref="ServiceDescriptor"/> for an additional <see cref="ILoggerProvider"/> to use.
		/// </summary>
		static ServiceDescriptor? additionalLoggerProvider;

		/// <summary>
		/// Initializes static members of the <see cref="ServiceCollectionExtensions"/> class.
		/// </summary>
		static ServiceCollectionExtensions()
		{
			UseDefaultServices();
		}

		/// <summary>
		/// Change the <see cref="Type"/> used as an implementation for calls to <see cref="AddChatProviderFactory(IServiceCollection)"/>.
		/// </summary>
		/// <typeparam name="TProviderFactory">The <see cref="IProviderFactory"/> implementation to use.</typeparam>
		public static void UseChatProviderFactory<TProviderFactory>()
			where TProviderFactory : IProviderFactory
		{
			chatProviderFactoryType = typeof(TProviderFactory);
		}

		/// <summary>
		/// Change the <see cref="Type"/> used as an implementation for calls to <see cref="AddGitHub(IServiceCollection)"/>.
		/// </summary>
		/// <typeparam name="TGitHubServiceFactory">The <see cref="IGitHubServiceFactory"/> implementation to use.</typeparam>
		public static void UseGitHubServiceFactory<TGitHubServiceFactory>()
			where TGitHubServiceFactory : IGitHubServiceFactory
		{
			gitHubServiceFactoryType = typeof(TGitHubServiceFactory);
		}

		/// <summary>
		/// Change the <see cref="Type"/> used as an implementation for calls to <see cref="AddGitHub(IServiceCollection)"/>.
		/// </summary>
		/// <typeparam name="TFileDownloader">The <see cref="IFileDownloader"/> implementation to use.</typeparam>
		public static void UseFileDownloader<TFileDownloader>()
			where TFileDownloader : IFileDownloader
		{
			fileDownloaderType = typeof(TFileDownloader);
		}

		/// <summary>
		/// Adds a <see cref="IFileDownloader"/> implementation to the given <paramref name="serviceCollection"/>.
		/// </summary>
		/// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
		/// <returns><paramref name="serviceCollection"/>.</returns>
		public static IServiceCollection AddFileDownloader(this IServiceCollection serviceCollection)
		{
			ArgumentNullException.ThrowIfNull(serviceCollection);

			serviceCollection.AddSingleton(typeof(IFileDownloader), fileDownloaderType ?? throw new InvalidOperationException("fileDownloaderType not set!"));

			return serviceCollection;
		}

		/// <summary>
		/// Adds a <see cref="IGitHubServiceFactory"/> implementation to the given <paramref name="serviceCollection"/>.
		/// </summary>
		/// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
		/// <returns><paramref name="serviceCollection"/>.</returns>
		public static IServiceCollection AddGitHub(this IServiceCollection serviceCollection)
		{
			ArgumentNullException.ThrowIfNull(serviceCollection);

			serviceCollection.AddSingleton<IGitHubClientFactory, GitHubClientFactory>();
			serviceCollection.AddSingleton(typeof(IGitHubServiceFactory), gitHubServiceFactoryType ?? throw new InvalidOperationException("gitHubServiceFactoryType not set!"));

			return serviceCollection;
		}

		/// <summary>
		/// Add an additional <see cref="ILoggerProvider"/> to <see cref="IServiceCollection"/>s that call <see cref="SetupLogging(IServiceCollection, Action{LoggerConfiguration}, Action{LoggerSinkConfiguration}, ElasticsearchSinkOptions, InternalConfiguration, FileLoggingConfiguration)"/>.
		/// </summary>
		/// <typeparam name="TLoggerProvider">The <see cref="Type"/> of <see cref="ILoggerProvider"/> to add.</typeparam>
		public static void UseAdditionalLoggerProvider<TLoggerProvider>()
			where TLoggerProvider : class, ILoggerProvider
		{
			if (additionalLoggerProvider != null)
				throw new InvalidOperationException("Cannot have multiple additionalLoggerProviders!");
			additionalLoggerProvider = ServiceDescriptor.Singleton<ILoggerProvider, TLoggerProvider>();
		}

		/// <summary>
		/// Adds a <see cref="IProviderFactory"/> implementation to the given <paramref name="serviceCollection"/>.
		/// </summary>
		/// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
		/// <returns><paramref name="serviceCollection"/>.</returns>
		public static IServiceCollection AddChatProviderFactory(this IServiceCollection serviceCollection)
		{
			ArgumentNullException.ThrowIfNull(serviceCollection);

			return serviceCollection.AddSingleton(typeof(IProviderFactory), chatProviderFactoryType ?? throw new InvalidOperationException("chatProviderFactoryType not set!"));
		}

		/// <summary>
		/// Add a standard <typeparamref name="TConfig"/> binding.
		/// </summary>
		/// <typeparam name="TConfig">The <see langword="class"/> to bind. Must have a <see langword="public"/> const/static <see cref="string"/> field named "Section".</typeparam>
		/// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
		/// <param name="configuration">The <see cref="IConfiguration"/> containing the <typeparamref name="TConfig"/>.</param>
		/// <returns><paramref name="serviceCollection"/>.</returns>
		public static OptionsBuilder<TConfig> UseStandardConfig<TConfig>(this IServiceCollection serviceCollection, IConfiguration configuration)
			where TConfig : class
		{
			ArgumentNullException.ThrowIfNull(serviceCollection);
			ArgumentNullException.ThrowIfNull(configuration);

			const string SectionFieldName = nameof(GeneralConfiguration.Section);

			var configType = typeof(TConfig);
			var sectionField = configType.GetField(SectionFieldName) ?? throw new InvalidOperationException(
				String.Format(CultureInfo.InvariantCulture, "{0} has no {1} field!", configType, SectionFieldName));
			var stringType = typeof(string);
			if (sectionField.FieldType != stringType)
				throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "{0} has invalid {1} field type, must be {2}!", configType, SectionFieldName, stringType));

			var sectionName = (string)sectionField.GetValue(null)!;

			return serviceCollection.AddOptionsWithValidateOnStart<TConfig>()
				.BindConfiguration(sectionName);
		}

		/// <summary>
		/// Clear previous providers and configure logging.
		/// </summary>
		/// <param name="serviceCollection">The <see cref="IServiceCollection"/> to configure.</param>
		/// <param name="configurationAction">Additional configuration for a given <see cref="LoggerConfiguration"/>.</param>
		/// <param name="sinkConfigurationAction">Additional configuration for a given <see cref="LoggerSinkConfiguration"/>.</param>
		/// <param name="elasticsearchSinkOptions">The <see cref="ElasticsearchSinkOptions"/> to use, if any.</param>
		/// <param name="internalConfiguration">The active <see cref="InternalConfiguration"/>, if any.</param>
		/// <param name="fileLoggingConfiguration">The active <see cref="FileLoggingConfiguration"/>, if any. Must be set if <paramref name="internalConfiguration"/> is passed in.</param>
		/// <returns>The updated <paramref name="serviceCollection"/>.</returns>
		public static IServiceCollection SetupLogging(
			this IServiceCollection serviceCollection,
			Action<LoggerConfiguration> configurationAction,
			Action<LoggerSinkConfiguration>? sinkConfigurationAction = null,
			ElasticsearchSinkOptions? elasticsearchSinkOptions = null,
			InternalConfiguration? internalConfiguration = null,
			FileLoggingConfiguration? fileLoggingConfiguration = null)
		{
			if (internalConfiguration != null)
				ArgumentNullException.ThrowIfNull(fileLoggingConfiguration);

			return serviceCollection.AddLogging(builder =>
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
						var template = "[{Timestamp:HH:mm:ss}] {Level:w3}: {SourceContext:l} ("
								+ SerilogContextHelper.Template
								+ "){NewLine}    {Message:lj}{NewLine}{Exception}";

						if (!((internalConfiguration?.UsingSystemD ?? false) && !(fileLoggingConfiguration?.Disable ?? false)))
							sinkConfiguration.Console(outputTemplate: template, formatProvider: CultureInfo.InvariantCulture);
						sinkConfigurationAction?.Invoke(sinkConfiguration);
					});

				if (elasticsearchSinkOptions != null)
					configuration.WriteTo.Elasticsearch(elasticsearchSinkOptions);

				builder.AddSerilog(configuration.CreateLogger(), true);

				if (Debugger.IsAttached)
					builder.AddDebug();

				if (additionalLoggerProvider != null)
					builder.Services.TryAddEnumerable(additionalLoggerProvider);
			});
		}

		/// <summary>
		/// Attempt to add the given <typeparamref name="THub"/> to services.
		/// </summary>
		/// <typeparam name="THub">The <see cref="Type"/> of the <see cref="Microsoft.AspNetCore.SignalR.Hub{T}"/> being added.</typeparam>
		/// <typeparam name="THubMethods">The implementation <see cref="Type"/> of the <typeparamref name="THub"/>.</typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/> to add the <typeparamref name="THub"/> to.</param>
		public static void AddHub<THub, THubMethods>(this IServiceCollection services)
			where THub : ConnectionMappingHub<THub, THubMethods>
			where THubMethods : class
		{
			ArgumentNullException.ThrowIfNull(services);

			services.TryAddSingleton(typeof(ComprehensiveHubContext<,>));
			services.AddSingleton<IConnectionMappedHubContext<THub, THubMethods>>(provider => provider.GetRequiredService<ComprehensiveHubContext<THub, THubMethods>>());
			services.AddSingleton<IHubConnectionMapper<THub, THubMethods>>(provider => provider.GetRequiredService<ComprehensiveHubContext<THub, THubMethods>>());
		}

		/// <summary>
		/// Set the modifiable services to their default types.
		/// </summary>
		static void UseDefaultServices()
		{
			UseChatProviderFactory<ProviderFactory>();
			UseGitHubServiceFactory<GitHubServiceFactory>();
			UseFileDownloader<FileDownloader>();
		}
	}
}
