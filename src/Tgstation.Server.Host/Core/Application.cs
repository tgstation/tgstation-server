using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

using Cyberboss.AspNetCore.AsyncInitializer;

using Elastic.CommonSchema.Serilog;

using HotChocolate.AspNetCore;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

using Newtonsoft.Json;

using Prometheus;

using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.Elasticsearch;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Hubs;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Authority;
using Tgstation.Server.Host.Authority.Core;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment.Remote;
using Tgstation.Server.Host.Components.Engine;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Controllers.Results;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.GraphQL.Subscriptions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Properties;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Security.OAuth;
using Tgstation.Server.Host.Setup;
using Tgstation.Server.Host.Swarm;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Transfer;
using Tgstation.Server.Host.Utils;
using Tgstation.Server.Shared;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Sets up dependency injection.
	/// </summary>
#pragma warning disable CA1506
	public sealed class Application : SetupApplication
	{
		/// <summary>
		/// The <see cref="IWebHostEnvironment"/> for the <see cref="Application"/>.
		/// </summary>
		readonly IWebHostEnvironment hostingEnvironment;

		/// <summary>
		/// The <see cref="ITokenFactory"/> for the <see cref="Application"/>.
		/// </summary>
		ITokenFactory? tokenFactory;

		/// <summary>
		/// Create the default <see cref="IServerFactory"/>.
		/// </summary>
		/// <returns>A new <see cref="IServerFactory"/> with the default settings.</returns>
		public static IServerFactory CreateDefaultServerFactory()
		{
			var assemblyInformationProvider = new AssemblyInformationProvider();
			var fileSystem = new FileSystem();
			var ioManager = new DefaultIOManager(fileSystem);
			return new ServerFactory(
				assemblyInformationProvider,
				ioManager,
				fileSystem);
		}

		/// <summary>
		/// Adds the <see cref="IWatchdogFactory"/> implementation.
		/// </summary>
		/// <typeparam name="TSystemWatchdogFactory">The <see cref="WatchdogFactory"/> child <see langword="class"/> for the current system.</typeparam>
		/// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
		/// <param name="postSetupServices">The <see cref="IPostSetupServices"/> to use.</param>
		static void AddWatchdog<TSystemWatchdogFactory>(IServiceCollection services, IPostSetupServices postSetupServices)
			where TSystemWatchdogFactory : class, IWatchdogFactory
		{
			if (postSetupServices.GeneralConfiguration.UseBasicWatchdog)
				services.AddSingleton<IWatchdogFactory, WatchdogFactory>();
			else
				services.AddSingleton<IWatchdogFactory, TSystemWatchdogFactory>();
		}

		/// <summary>
		/// Get the OIDC authentication scheme name for a given <paramref name="schemeKey"/>.
		/// </summary>
		/// <param name="schemeKey">The scheme key for the <see cref="OidcConfiguration"/>.</param>
		/// <returns>The authentication scheme name.</returns>
		static string GetOidcScheme(string schemeKey)
			=> AuthenticationContextFactory.OpenIDConnectAuthenticationSchemePrefix + schemeKey;

		/// <summary>
		/// Initializes a new instance of the <see cref="Application"/> class.
		/// </summary>
		/// <param name="configuration">The <see cref="IConfiguration"/> for the <see cref="SetupApplication"/>.</param>
		/// <param name="hostingEnvironment">The <see cref="IWebHostEnvironment"/> for the <see cref="SetupApplication"/>.</param>
		public Application(
			IConfiguration configuration,
			IWebHostEnvironment hostingEnvironment)
			: base(configuration)
		{
			this.hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
		}

		/// <summary>
		/// Configure the <see cref="Application"/>'s <paramref name="services"/>.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> needed for configuration.</param>
		/// <param name="ioManager">The <see cref="IIOManager"/> needed for configuration.</param>
		/// <param name="postSetupServices">The <see cref="IPostSetupServices"/> needed for configuration.</param>
		/// <param name="fileSystem">The <see cref="IFileSystem"/> needed for configuration.</param>
		public void ConfigureServices(
			IServiceCollection services,
			IAssemblyInformationProvider assemblyInformationProvider,
			IIOManager ioManager,
			IPostSetupServices postSetupServices,
			IFileSystem fileSystem)
		{
			ConfigureServices(services, assemblyInformationProvider, ioManager);

			ArgumentNullException.ThrowIfNull(postSetupServices);

			// configure configuration
			services.UseStandardConfig<UpdatesConfiguration>(Configuration);
			services.UseStandardConfig<ControlPanelConfiguration>(Configuration);
			services.UseStandardConfig<SwarmConfiguration>(Configuration);
			services.UseStandardConfig<SessionConfiguration>(Configuration);

			// Set the timeout for IHostedService.StopAsync
			services.Configure<HostOptions>(
				opts => opts.ShutdownTimeout = TimeSpan.FromMinutes(postSetupServices.GeneralConfiguration.RestartTimeoutMinutes));

			static LogEventLevel? ConvertSeriLogLevel(LogLevel logLevel) =>
				logLevel switch
				{
					LogLevel.Critical => LogEventLevel.Fatal,
					LogLevel.Debug => LogEventLevel.Debug,
					LogLevel.Error => LogEventLevel.Error,
					LogLevel.Information => LogEventLevel.Information,
					LogLevel.Trace => LogEventLevel.Verbose,
					LogLevel.Warning => LogEventLevel.Warning,
					LogLevel.None => null,
					_ => throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid log level {0}", logLevel)),
				};

			var microsoftEventLevel = ConvertSeriLogLevel(postSetupServices.FileLoggingConfiguration.MicrosoftLogLevel);
			var elasticsearchConfiguration = postSetupServices.ElasticsearchConfiguration;
			services.SetupLogging(
				config =>
				{
					if (microsoftEventLevel.HasValue)
					{
						config.MinimumLevel.Override("Microsoft", microsoftEventLevel.Value);
						config.MinimumLevel.Override("System.Net.Http.HttpClient", microsoftEventLevel.Value);
					}
				},
				sinkConfig =>
				{
					if (postSetupServices.FileLoggingConfiguration.Disable)
						return;

					var logPath = postSetupServices.FileLoggingConfiguration.GetFullLogDirectory(
						ioManager,
						assemblyInformationProvider,
						postSetupServices.PlatformIdentifier);

					var logEventLevel = ConvertSeriLogLevel(postSetupServices.FileLoggingConfiguration.LogLevel);

					var formatter = new MessageTemplateTextFormatter(
						"{Timestamp:o} "
						+ SerilogContextHelper.Template
						+ "): [{Level:u3}] {SourceContext:l}: {Message} ({EventId:x8}){NewLine}{Exception}",
						null);

					logPath = ioManager.ConcatPath(logPath, "tgs-.log");
					var rollingFileConfig = sinkConfig.File(
						formatter,
						logPath,
						logEventLevel ?? LogEventLevel.Verbose,
						50 * 1024 * 1024, // 50MB max size
						flushToDiskInterval: TimeSpan.FromSeconds(2),
						rollingInterval: RollingInterval.Day,
						rollOnFileSizeLimit: true);
				},
				elasticsearchConfiguration.Enable
					? new ElasticsearchSinkOptions(elasticsearchConfiguration.Host ?? throw new InvalidOperationException($"Missing {ElasticsearchConfiguration.Section}:{nameof(elasticsearchConfiguration.Host)}!"))
					{
						// Yes I know this means they cannot use a self signed cert unless they also have authentication, but lets be real here
						// No one is going to be doing one of those but not the other
						ModifyConnectionSettings = connectionConfigration => (!String.IsNullOrWhiteSpace(elasticsearchConfiguration.Username) && !String.IsNullOrWhiteSpace(elasticsearchConfiguration.Password))
							? connectionConfigration
								.BasicAuthentication(
									elasticsearchConfiguration.Username,
									elasticsearchConfiguration.Password)
								.ServerCertificateValidationCallback((o, certificate, chain, errors) => true)
							: null,
						CustomFormatter = new EcsTextFormatter(),
						AutoRegisterTemplate = true,
						AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
						IndexFormat = "tgs-logs",
					}
					: null,
				postSetupServices.InternalConfiguration,
				postSetupServices.FileLoggingConfiguration);

			// configure authentication pipeline
			ConfigureAuthenticationPipeline(services, postSetupServices.SecurityConfiguration);

			// add mvc, configure the json serializer settings
			var jsonVersionConverterList = new List<JsonConverter>
			{
				new VersionConverter(),
			};

			void ConfigureNewtonsoftJsonSerializerSettingsForApi(JsonSerializerSettings settings)
			{
				settings.NullValueHandling = NullValueHandling.Ignore;
				settings.CheckAdditionalContent = true;
				settings.MissingMemberHandling = MissingMemberHandling.Error;
				settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
				settings.Converters = jsonVersionConverterList;
			}

			services
				.AddMvc(options =>
				{
					options.ReturnHttpNotAcceptable = true;
					options.RespectBrowserAcceptHeader = true;
				})
				.AddNewtonsoftJson(options =>
				{
					options.AllowInputFormatterExceptionMessages = true;
					ConfigureNewtonsoftJsonSerializerSettingsForApi(options.SerializerSettings);
				});

			services.AddSignalR(
				options =>
				{
					options.AddFilter<AuthorizationContextHubFilter>();
				})
				.AddNewtonsoftJsonProtocol(options =>
				{
					ConfigureNewtonsoftJsonSerializerSettingsForApi(options.PayloadSerializerSettings);
				});

			services.AddHub<JobsHub, IJobsHub>();

			if (postSetupServices.GeneralConfiguration.HostApiDocumentation)
			{
				string GetDocumentationFilePath(string assemblyLocation) => ioManager.ConcatPath(ioManager.GetDirectoryName(assemblyLocation), String.Concat(ioManager.GetFileNameWithoutExtension(assemblyLocation), ".xml"));
				var assemblyDocumentationPath = GetDocumentationFilePath(GetType().Assembly.Location);
				var apiDocumentationPath = GetDocumentationFilePath(typeof(ApiHeaders).Assembly.Location);
				services.AddSwaggerGen(genOptions => SwaggerConfiguration.Configure(genOptions, assemblyDocumentationPath, apiDocumentationPath));
				services.AddSwaggerGenNewtonsoftSupport();
			}

			// CORS conditionally enabled later
			services.AddCors();

			// Enable managed HTTP clients
			services
				.AddHttpClient()
				.ConfigureHttpClientDefaults(
					builder => builder.ConfigureHttpClient(
						client => client.DefaultRequestHeaders.UserAgent.Add(
							assemblyInformationProvider.ProductInfoHeaderValue)));

			services.AddSingleton<IMetricFactory>(_ => Metrics.DefaultFactory);
			services.AddSingleton<ICollectorRegistry>(_ => Metrics.DefaultRegistry);

			services.UseHttpClientMetrics();

			var healthChecksBuilder = services
				.AddHealthChecks()
				.ForwardToPrometheus();

			// configure graphql
			services
				.AddScoped<ITopicEventReceiver, ShutdownAwareTopicEventReceiver>()
				.AddGraphQLServer()
				.ConfigureGraphQLServer();

			void AddTypedContext<TContext>()
				where TContext : DatabaseContext
			{
				var configureAction = DatabaseContext.GetConfigureAction<TContext>();

				services.AddDbContextPool<TContext>((serviceProvider, builder) =>
				{
					if (hostingEnvironment.IsDevelopment())
						builder.EnableSensitiveDataLogging();

					var databaseConfigOptions = serviceProvider.GetRequiredService<IOptions<DatabaseConfiguration>>();
					var databaseConfig = databaseConfigOptions.Value ?? throw new InvalidOperationException("DatabaseConfiguration missing!");
					configureAction(builder, databaseConfig);
				});
				services.AddScoped<IDatabaseContext>(x => x.GetRequiredService<TContext>());

				healthChecksBuilder
					.AddDbContextCheck<TContext>();
			}

			// add the correct database context type
			var dbType = postSetupServices.DatabaseConfiguration.DatabaseType;
			switch (dbType)
			{
				case DatabaseType.MySql:
				case DatabaseType.MariaDB:
					AddTypedContext<MySqlDatabaseContext>();
					break;
				case DatabaseType.SqlServer:
					AddTypedContext<SqlServerDatabaseContext>();
					break;
				case DatabaseType.Sqlite:
					AddTypedContext<SqliteDatabaseContext>();
					break;
				case DatabaseType.PostgresSql:
					AddTypedContext<PostgresSqlDatabaseContext>();
					break;
				default:
					throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid {0}: {1}!", nameof(DatabaseType), dbType));
			}

			// configure other database services
			services.AddSingleton<IDatabaseContextFactory, DatabaseContextFactory>();
			services.AddSingleton<IDatabaseSeeder, DatabaseSeeder>();

			// configure other security services
			services.AddScoped<IOAuthProviders, OAuthProviders>();
			services.AddSingleton<IIdentityCache, IdentityCache>();
			services.AddSingleton<ICryptographySuite, CryptographySuite>();
			services.AddSingleton<ITokenFactory, TokenFactory>();
			services.AddSingleton<ISessionInvalidationTracker, SessionInvalidationTracker>();
			services.AddSingleton<IPasswordHasher<Models.User>, PasswordHasher<Models.User>>();

			// configure platform specific services
			if (postSetupServices.PlatformIdentifier.IsWindows)
			{
				AddWatchdog<WindowsWatchdogFactory>(services, postSetupServices);
				services.AddSingleton<ISystemIdentityFactory, WindowsSystemIdentityFactory>();
				services.AddSingleton<IFilesystemLinkFactory, WindowsFilesystemLinkFactory>();
				services.AddSingleton<ByondInstallerBase, WindowsByondInstaller>();
				services.AddSingleton<OpenDreamInstaller, WindowsOpenDreamInstaller>();
				services.AddSingleton<IPostWriteHandler, WindowsPostWriteHandler>();
				services.AddSingleton<IProcessFeatures, WindowsProcessFeatures>();

				services.AddSingleton<WindowsNetworkPromptReaper>();
				services.AddSingleton<INetworkPromptReaper>(x => x.GetRequiredService<WindowsNetworkPromptReaper>());
				services.AddSingleton<IHostedService>(x => x.GetRequiredService<WindowsNetworkPromptReaper>());
			}
			else
			{
				AddWatchdog<PosixWatchdogFactory>(services, postSetupServices);
				services.AddSingleton<ISystemIdentityFactory, PosixSystemIdentityFactory>();
				services.AddSingleton<IFilesystemLinkFactory, PosixFilesystemLinkFactory>();
				services.AddSingleton<ByondInstallerBase, PosixByondInstaller>();
				services.AddSingleton<OpenDreamInstaller>();
				services.AddSingleton<IPostWriteHandler, PosixPostWriteHandler>();

				services.AddSingleton<IProcessFeatures, PosixProcessFeatures>();
				services.AddHostedService<PosixProcessFeatures>();

				// PosixProcessFeatures also needs a IProcessExecutor for gcore
				services.AddSingleton(x => new Lazy<IProcessExecutor>(() => x.GetRequiredService<IProcessExecutor>(), true));
				services.AddSingleton<INetworkPromptReaper, PosixNetworkPromptReaper>();

				services.AddHostedService<PosixSignalHandler>();
			}

			// only global repo manager should be for the OD repo
			// god help me if we need more
			var openDreamRepositoryDirectory = ioManager.ConcatPath(
				ioManager.GetPathInLocalDirectory(assemblyInformationProvider),
				"OpenDreamRepository");
			services.AddSingleton(
				services => services
					.GetRequiredService<IRepositoryManagerFactory>()
					.CreateRepositoryManager(
						services.GetRequiredService<IIOManager>().CreateResolverForSubdirectory(
							openDreamRepositoryDirectory),
						new NoopEventConsumer()));

			services.AddSingleton(
				serviceProvider => new Dictionary<EngineType, IEngineInstaller>
				{
					{ EngineType.Byond, serviceProvider.GetRequiredService<ByondInstallerBase>() },
					{ EngineType.OpenDream, serviceProvider.GetRequiredService<OpenDreamInstaller>() },
				}
				.ToFrozenDictionary());
			services.AddSingleton<IEngineInstaller, DelegatingEngineInstaller>();

			if (postSetupServices.InternalConfiguration.UsingSystemD)
				services.AddHostedService<SystemDManager>();

			// configure file transfer services
			services.AddSingleton<FileTransferService>();
			services.AddSingleton<IFileTransferStreamHandler>(x => x.GetRequiredService<FileTransferService>());
			services.AddSingleton<IFileTransferTicketProvider>(x => x.GetRequiredService<FileTransferService>());
			services.AddTransient<IActionResultExecutor<LimitedStreamResult>, LimitedStreamResultExecutor>();

			// configure swarm service
			services.AddSingleton<SwarmService>();
			services.AddSingleton<ISwarmService>(x => x.GetRequiredService<SwarmService>());
			services.AddSingleton<ISwarmOperations>(x => x.GetRequiredService<SwarmService>());
			services.AddSingleton<ISwarmServiceController>(x => x.GetRequiredService<SwarmService>());

			if (postSetupServices.SwarmConfiguration.PrivateKey != null)
				services.AddGrpc();

			// configure component services
			services.AddSingleton<IPortAllocator, PortAllocator>();
			services.AddSingleton<IInstanceFactory, InstanceFactory>();
			services.AddSingleton<IGitRemoteFeaturesFactory, GitRemoteFeaturesFactory>();
			services.AddSingleton<ILibGit2RepositoryFactory, LibGit2RepositoryFactory>();
			services.AddSingleton<ILibGit2Commands, LibGit2Commands>();
			services.AddSingleton<IRepositoryManagerFactory, RepostoryManagerFactory>();
			services.AddSingleton<IRemoteDeploymentManagerFactory, RemoteDeploymentManagerFactory>();
			services.AddChatProviderFactory();
			services.AddSingleton<IChatManagerFactory, ChatManagerFactory>();
			services.AddSingleton<IServerUpdater, ServerUpdater>();
			services.AddSingleton<IServerUpdateInitiator, ServerUpdateInitiator>();
			services.AddSingleton<IDotnetDumpService, DotnetDumpService>();

			// configure authorities
			services.AddScoped(typeof(IRestAuthorityInvoker<>), typeof(RestAuthorityInvoker<>));
			services.AddScoped(typeof(IGraphQLAuthorityInvoker<>), typeof(GraphQLAuthorityInvoker<>));
			services.AddScoped<ILoginAuthority, LoginAuthority>();
			services.AddScoped<IUserAuthority, UserAuthority>();
			services.AddScoped<IUserGroupAuthority, UserGroupAuthority>();
			services.AddScoped<IPermissionSetAuthority, PermissionSetAuthority>();
			services.AddScoped<IAdministrationAuthority, AdministrationAuthority>();
			services.AddScoped<IChatAuthority, ChatAuthority>();

			// configure misc services
			services.AddSingleton<IProcessExecutor, ProcessExecutor>();
			services.AddSingleton<ISynchronousIOManager, SynchronousIOManager>();
			services.AddSingleton<ITopicClientFactory, TopicClientFactory>();
			services.AddSingleton<ICallInvokerFactory, CallInvokerFactory>();
			services.AddSingleton<BridgeController>();
			services.AddSingleton(fileSystem);
			services.AddHostedService<CommandPipeManager>();

			services.AddFileDownloader();
			services.AddGitHub();

			// configure root services
			services.AddSingleton<JobService>();
			services.AddSingleton<IJobService>(provider => provider.GetRequiredService<JobService>());
			services.AddSingleton<IJobsHubUpdater>(provider => provider.GetRequiredService<JobService>());
			services.AddSingleton<IJobManager>(x => x.GetRequiredService<IJobService>());
			services.AddSingleton<JobsHubGroupMapper>();
			services.AddSingleton<IPermissionsUpdateNotifyee>(provider => provider.GetRequiredService<JobsHubGroupMapper>());
			services.AddSingleton<IHostedService>(x => x.GetRequiredService<JobsHubGroupMapper>()); // bit of a hack, but we need this to load immediated

			services.AddSingleton<InstanceManager>();
			services.AddSingleton<IBridgeDispatcher>(x => x.GetRequiredService<InstanceManager>());
			services.AddSingleton<IInstanceManager>(x => x.GetRequiredService<InstanceManager>());
		}

		/// <summary>
		/// Configure the <see cref="Application"/>.
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure.</param>
		/// <param name="serverControl">The <see cref="IServerControl"/> for the <see cref="Application"/>.</param>
		/// <param name="tokenFactory">The value of <see cref="tokenFactory"/>.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/>.</param>
		/// <param name="controlPanelConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="ControlPanelConfiguration"/> to use.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="GeneralConfiguration"/> to use.</param>
		/// <param name="databaseConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="DatabaseConfiguration"/> to use.</param>
		/// <param name="securityConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="SecurityConfiguration"/> to use.</param>
		/// <param name="swarmConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="SwarmConfiguration"/> to use.</param>
		/// <param name="internalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="InternalConfiguration"/> to use.</param>
		/// <param name="sessionConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="SessionConfiguration"/> to use.</param>
		/// <param name="logger">The <see cref="Microsoft.Extensions.Logging.ILogger"/> for the <see cref="Application"/>.</param>
#pragma warning disable CA1502 // TODO: Decomplexify
		public void Configure(
			IApplicationBuilder applicationBuilder,
			IServerControl serverControl,
			ITokenFactory tokenFactory,
			IAssemblyInformationProvider assemblyInformationProvider,
			IOptions<ControlPanelConfiguration> controlPanelConfigurationOptions,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<DatabaseConfiguration> databaseConfigurationOptions,
			IOptions<SecurityConfiguration> securityConfigurationOptions,
			IOptions<SwarmConfiguration> swarmConfigurationOptions,
			IOptions<InternalConfiguration> internalConfigurationOptions,
			IOptions<SessionConfiguration> sessionConfigurationOptions,
			ILogger<Application> logger)
#pragma warning restore CA1502
		{
			ArgumentNullException.ThrowIfNull(applicationBuilder);
			ArgumentNullException.ThrowIfNull(serverControl);

			this.tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));

			ArgumentNullException.ThrowIfNull(assemblyInformationProvider);

			var controlPanelConfiguration = (controlPanelConfigurationOptions ?? throw new ArgumentNullException(nameof(controlPanelConfigurationOptions))).Value;
			var generalConfiguration = (generalConfigurationOptions ?? throw new ArgumentNullException(nameof(generalConfigurationOptions))).Value;
			var databaseConfiguration = (databaseConfigurationOptions ?? throw new ArgumentNullException(nameof(databaseConfigurationOptions))).Value;
			var swarmConfiguration = (swarmConfigurationOptions ?? throw new ArgumentNullException(nameof(swarmConfigurationOptions))).Value;
			var internalConfiguration = (internalConfigurationOptions ?? throw new ArgumentNullException(nameof(internalConfigurationOptions))).Value;
			var sessionConfiguration = (sessionConfigurationOptions ?? throw new ArgumentNullException(nameof(sessionConfigurationOptions))).Value;

			ArgumentNullException.ThrowIfNull(logger);

			logger.LogDebug("Database provider: {provider}", databaseConfiguration.DatabaseType);
			logger.LogDebug("Content Root: {contentRoot}", hostingEnvironment.ContentRootPath);
			logger.LogTrace("Web Root: {webRoot}", hostingEnvironment.WebRootPath);

			// setup the HTTP request pipeline
			// Add additional logging context to the request
			applicationBuilder.UseAdditionalRequestLoggingContext(swarmConfiguration);

			// Wrap exceptions in a 500 (ErrorMessage) response
			applicationBuilder.UseServerErrorHandling();

			// header forwarding important for OIDC
			var forwardedHeaderOptions = new ForwardedHeadersOptions
			{
				ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
			};

			forwardedHeaderOptions.KnownIPNetworks.Clear();
			forwardedHeaderOptions.KnownIPNetworks.Add(
				new global::System.Net.IPNetwork(
					IPAddress.Any,
					0));

			applicationBuilder.UseForwardedHeaders(forwardedHeaderOptions);

			// metrics capture
			applicationBuilder.UseHttpMetrics();

			// Add the X-Powered-By response header
			applicationBuilder.UseServerBranding(assemblyInformationProvider);

			// Add the X-Accel-Buffering response header
			applicationBuilder.UseDisabledNginxProxyBuffering();

			// suppress OperationCancelledExceptions, they are just aborted HTTP requests
			applicationBuilder.UseCancelledRequestSuppression();

			// 503 requests made while the application is starting
			applicationBuilder.UseAsyncInitialization<IInstanceManager>(
				(instanceManager, cancellationToken) => instanceManager.Ready.WaitAsync(cancellationToken));

			if (generalConfiguration.HostApiDocumentation)
			{
				var siteDocPath = Routes.ApiRoot + $"doc/{SwaggerConfiguration.DocumentName}.json";
				if (!String.IsNullOrWhiteSpace(controlPanelConfiguration.PublicPath))
					siteDocPath = controlPanelConfiguration.PublicPath.TrimEnd('/') + siteDocPath;

				applicationBuilder.UseSwagger(options =>
				{
					options.RouteTemplate = Routes.ApiRoot + "doc/{documentName}.{json|yaml}";
				});
				applicationBuilder.UseSwaggerUI(options =>
				{
					options.RoutePrefix = SwaggerConfiguration.DocumentationSiteRouteExtension;
					options.SwaggerEndpoint(siteDocPath, "TGS API");
				});
				logger.LogTrace("Swagger API generation enabled");
			}

			// spa loading if necessary
			if (controlPanelConfiguration.Enable)
			{
				logger.LogInformation("Web control panel enabled.");
				applicationBuilder.UseFileServer(new FileServerOptions
				{
					RequestPath = ControlPanelController.ControlPanelRoute,
					EnableDefaultFiles = true,
					EnableDirectoryBrowsing = false,
					RedirectToAppendTrailingSlash = false,
				});
			}
			else
#if NO_WEBPANEL
				logger.LogDebug("Web control panel was not included in TGS build!");
#else
				logger.LogTrace("Web control panel disabled!");
#endif

			// Enable endpoint routing
			applicationBuilder.UseRouting();

			// Set up CORS based on configuration if necessary
			Action<CorsPolicyBuilder>? corsBuilder = null;
			if (controlPanelConfiguration.AllowAnyOrigin)
			{
				logger.LogTrace("Access-Control-Allow-Origin: *");
				corsBuilder = builder => builder.SetIsOriginAllowed(_ => true);
			}
			else if (controlPanelConfiguration.AllowedOrigins?.Count > 0)
			{
				logger.LogTrace("Access-Control-Allow-Origin: {allowedOrigins}", String.Join(',', controlPanelConfiguration.AllowedOrigins));
				corsBuilder = builder => builder.WithOrigins([.. controlPanelConfiguration.AllowedOrigins]);
			}

			var originalBuilder = corsBuilder;
			corsBuilder = builder =>
			{
				builder
					.AllowAnyHeader()
					.AllowAnyMethod()
					.AllowCredentials()
					.SetPreflightMaxAge(TimeSpan.FromDays(1));
				originalBuilder?.Invoke(builder);
			};
			applicationBuilder.UseCors(corsBuilder);

			// validate the API version
			applicationBuilder.UseApiCompatibility();

			// authenticate JWT tokens using our security pipeline if present, returns 401 if bad
			applicationBuilder.UseAuthentication();

			// enable authorization on endpoints
			applicationBuilder.UseAuthorization();

			// suppress and log database exceptions
			applicationBuilder.UseDbConflictHandling();

			// setup endpoints
			var httpApiHosts = generalConfiguration.ApiEndPoints.Select(endpoint => endpoint.ParseEndPointSpecification()).ToArray();
			var metricsHosts = generalConfiguration.MetricsEndPoints.Select(endpoint => endpoint.ParseEndPointSpecification()).ToArray();
			var bridgeHost = $"{IPAddress.Loopback}:{sessionConfiguration.BridgePort}";
			string[]? swarmHosts = null;
			applicationBuilder.UseEndpoints(endpoints =>
			{
				// access to the signalR jobs hub
				endpoints.MapHub<JobsHub>(
					Routes.JobsHub,
					options =>
					{
						options.Transports = HttpTransportType.ServerSentEvents;
						options.CloseOnAuthenticationExpiration = true;
					})
					.RequireAuthorization()
					.RequireCors(corsBuilder)
					.RequireHost(httpApiHosts);

				swarmHosts = SwarmEndpointsBuilder.Map(endpoints, swarmConfiguration);

				static Task ProcessBridgeRequest(HttpContext context) => context.RequestServices.GetRequiredService<BridgeController>().Process(context);

				endpoints.MapGet(
					$"/{BridgeController.RouteExtension}",
					ProcessBridgeRequest)
					.RequireHost(bridgeHost);

				endpoints.MapGet(
					$"{Routes.ApiRoot}{BridgeController.RouteExtension}",
					ProcessBridgeRequest)
					.RequireHost(bridgeHost);

				// majority of handling is done in the controllers
				endpoints.MapControllers()
					.RequireHost(httpApiHosts);

				if (internalConfiguration.EnableGraphQL)
				{
					logger.LogWarning("Enabling GraphQL. This API is experimental and breaking changes may occur at any time!");
					var gqlOptions = new GraphQLServerOptions
					{
						EnableBatching = true,
					};

					gqlOptions.Tool.Enable = generalConfiguration.HostApiDocumentation;

					endpoints
						.MapGraphQL(Routes.GraphQL)
						.WithOptions(gqlOptions)
						.RequireHost(httpApiHosts);
				}

				if (generalConfiguration.MetricsEndPoints.Count > 0)
					endpoints
						.MapMetrics()
						.RequireHost();
				else
					logger.LogTrace("Prometheus disabled");

				endpoints.MapHealthChecks("/health")
					.RequireHost(httpApiHosts);

				var oidcConfig = securityConfigurationOptions.Value.OpenIDConnect;
				if (oidcConfig == null)
					return;

				foreach (var kvp in oidcConfig)
					endpoints.MapGet(
						$"/oidc/{kvp.Key}/signin",
						context => context.ChallengeAsync(
							GetOidcScheme(kvp.Key),
							new AuthenticationProperties
							{
								RedirectUri = $"/oidc/{kvp.Key}/landing",
							}))
						.RequireHost(httpApiHosts);
			});

			// 404 anything that gets this far
			// End of request pipeline setup
			logger.LogTrace("Configuration version: {configVersion}", GeneralConfiguration.CurrentConfigVersion);
			logger.LogTrace("DMAPI Interop version: {interopVersion}", DMApiConstants.InteropVersion);
			if (controlPanelConfiguration.Enable)
				logger.LogTrace("Webpanel version: {webCPVersion}", MasterVersionsAttribute.Instance.RawWebpanelVersion);

			logger.LogDebug("Bridge service hosted on: {bridgeHost}", bridgeHost);
			if (metricsHosts.Any())
				logger.LogDebug("Metrics service hosted on: {metricsHosts}", String.Join(", ", metricsHosts));

			if (swarmHosts != null)
				logger.LogDebug("Swarm service hosted on: {swarmHosts}", String.Join(", ", swarmHosts));

			logger.LogDebug("HTTP API hosted on: {httpApiHosts}", String.Join(", ", httpApiHosts));
		}

		/// <inheritdoc />
		protected override void ConfigureHostedService(IServiceCollection services)
			=> services.AddSingleton<IHostedService>(x => x.GetRequiredService<InstanceManager>());

		/// <inheritdoc />
		protected override void UseValidatedConfig<TConfig, TValidator>(IServiceCollection services)
			=> services.UseValidatedConfig<TConfig, TValidator>(Configuration);

		/// <summary>
		/// Configure the <paramref name="services"/> for the authentication pipeline.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
		/// <param name="securityConfiguration">The <see cref="SecurityConfiguration"/>.</param>
		void ConfigureAuthenticationPipeline(IServiceCollection services, SecurityConfiguration securityConfiguration)
		{
			services.AddHttpContextAccessor();
			services.AddScoped<IApiHeadersProvider, ApiHeadersProvider>();
			services.AddScoped<AuthenticationContextFactory>();
			services.AddScoped<ITokenValidator>(provider => provider.GetRequiredService<AuthenticationContextFactory>());

			services.AddScoped<IClaimsPrincipalAccessor, ClaimsPrincipalAccessor>();
			services.AddScoped<Security.IAuthorizationService, AuthorizationService>();
			services.AddScoped<IAuthorizationHandler, AuthorizationHandler>();

			// what if you
			// wanted to just do this:
			// return provider.GetRequiredService<AuthenticationContextFactory>().CurrentAuthenticationContext
			// But M$ said
			// https://stackoverflow.com/questions/56792917/scoped-services-in-asp-net-core-with-signalr-hubs
			services.AddScoped(provider => (provider
				.GetRequiredService<IHttpContextAccessor>()
				.HttpContext ?? throw new InvalidOperationException($"Unable to resolve {nameof(IAuthenticationContext)} due to no HttpContext being available!"))
				.RequestServices
				.GetRequiredService<AuthenticationContextFactory>()
				.CurrentAuthenticationContext);
			services.AddScoped<IClaimsTransformation, AuthenticationContextClaimsTransformation>();

			var authBuilder = services
				.AddAuthentication(options =>
				{
					options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
				})
				.AddJwtBearer(jwtBearerOptions =>
				{
					// this line isn't actually run until the first request is made
					// at that point tokenFactory will be populated
					jwtBearerOptions.TokenValidationParameters = tokenFactory?.ValidationParameters ?? throw new InvalidOperationException("tokenFactory not initialized!");
					jwtBearerOptions.MapInboundClaims = false;
					jwtBearerOptions.Events = new JwtBearerEvents
					{
						OnMessageReceived = context =>
						{
							if (String.IsNullOrWhiteSpace(context.Token))
							{
								var accessToken = context.Request.Query["access_token"];
								var path = context.HttpContext.Request.Path;

								if (!String.IsNullOrWhiteSpace(accessToken) &&
									path.StartsWithSegments(Routes.HubsRoot, StringComparison.OrdinalIgnoreCase))
								{
									context.Token = accessToken;
								}
							}

							return Task.CompletedTask;
						},
						OnTokenValidated = context => context
							.HttpContext
							.RequestServices
							.GetRequiredService<ITokenValidator>()
							.ValidateTgsToken(
								context,
								context
									.HttpContext
									.RequestAborted),
					};
				});

			authBuilder.AddScheme<AuthenticationSchemeOptions, SwarmAuthenticationHandler>(SwarmConstants.AuthenticationSchemeAndPolicy, "Swarm Authentication", null);

			services.AddAuthorization(options =>
			{
				options.AddPolicy(
					TgsAuthorizeAttribute.PolicyName,
					builder => builder
						.RequireAuthenticatedUser()
						.RequireRole(TgsAuthorizeAttribute.UserEnabledRole));

				options.AddPolicy(
					SwarmConstants.AuthenticationSchemeAndPolicy,
					builder => builder
						.RequireAuthenticatedUser()
						.AddAuthenticationSchemes(SwarmConstants.AuthenticationSchemeAndPolicy));

				options.DefaultPolicy = options.GetPolicy(TgsAuthorizeAttribute.PolicyName)!;
			});

			var oidcConfig = securityConfiguration.OpenIDConnect;
			if (oidcConfig == null || oidcConfig.Count == 0)
				return;

			authBuilder.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

			foreach (var kvp in oidcConfig)
			{
				var configName = kvp.Key;
				authBuilder
					.AddOpenIdConnect(
						GetOidcScheme(configName),
						options =>
						{
							var config = kvp.Value;

							options.Authority = config.Authority?.ToString();
							options.ClientId = config.ClientId;
							options.ClientSecret = config.ClientSecret;

							options.Events = new OpenIdConnectEvents
							{
								OnRemoteFailure = context =>
								{
									context.HandleResponse();
									context.HttpContext.Response.Redirect($"{config.ReturnPath}?error={HttpUtility.UrlEncode(context.Failure?.Message ?? $"{options.Events.OnRemoteFailure} was called without an {nameof(Exception)}!")}&state=oidc.{HttpUtility.UrlEncode(configName)}");
									return Task.CompletedTask;
								},
								OnTicketReceived = context =>
								{
									var services = context
										.HttpContext
										.RequestServices;
									var tokenFactory = services
										.GetRequiredService<ITokenFactory>();
									var authenticationContext = services
										.GetRequiredService<IAuthenticationContext>();
									context.HandleResponse();
									context.HttpContext.Response.Redirect($"{config.ReturnPath}?code={HttpUtility.UrlEncode(tokenFactory.CreateToken(authenticationContext.User, true))}&state=oidc.{HttpUtility.UrlEncode(configName)}");
									return Task.CompletedTask;
								},
							};

							Task CompleteAuth(RemoteAuthenticationContext<OpenIdConnectOptions> context)
								=> context
										.HttpContext
										.RequestServices
										.GetRequiredService<ITokenValidator>()
										.ValidateOidcToken(
											context,
											configName,
											config.GroupIdClaim,
											context
												.HttpContext
												.RequestAborted);

							if (securityConfiguration.OidcStrictMode)
							{
								options.GetClaimsFromUserInfoEndpoint = true;
								options.ClaimActions.MapUniqueJsonKey(config.GroupIdClaim, config.GroupIdClaim);
								options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
								{
									NameClaimType = config.UsernameClaim,
									RoleClaimType = "roles",
								};

								options.Scope.Add(OpenIdConnectScope.Profile);
								options.Events.OnUserInformationReceived = CompleteAuth;
							}
							else
								options.Events.OnTokenValidated = CompleteAuth;

							options.Scope.Add(OpenIdConnectScope.OpenId);
							options.Scope.Add(OpenIdConnectScope.OfflineAccess);

							options.RequireHttpsMetadata = false;
							options.SaveTokens = true;
							options.ResponseType = OpenIdConnectResponseType.Code;
							options.MapInboundClaims = false;

							options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

							var basePath = $"/oidc/{configName}/";
							options.CallbackPath = new PathString(basePath + "signin-callback");
							options.SignedOutCallbackPath = new PathString(basePath + "signout-callback");
							options.RemoteSignOutPath = new PathString(basePath + "signout");
						});
			}
		}
	}
}
