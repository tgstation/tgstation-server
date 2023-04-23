using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

using Cyberboss.AspNetCore.AsyncInitializer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;

using Tgstation.Server.Api;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Chat.Providers;
using Tgstation.Server.Host.Components.Deployment.Remote;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Extensions.Converters;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Properties;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Security.OAuth;
using Tgstation.Server.Host.Setup;
using Tgstation.Server.Host.Swarm;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Transfer;

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
		ITokenFactory tokenFactory;

		/// <summary>
		/// Create the default <see cref="IServerFactory"/>.
		/// </summary>
		/// <returns>A new <see cref="IServerFactory"/> with the default settings.</returns>
		public static IServerFactory CreateDefaultServerFactory()
		{
			var assemblyInformationProvider = new AssemblyInformationProvider();
			var ioManager = new DefaultIOManager();
			return new ServerFactory(
				assemblyInformationProvider,
				ioManager);
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
		/// Configure the <see cref="Application"/>'s services.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> needed for configuration.</param>
		/// <param name="ioManager">The <see cref="IIOManager"/> needed for configuration.</param>
		/// <param name="postSetupServices">The <see cref="IPostSetupServices"/> needed for configuration.</param>
		public void ConfigureServices(
			IServiceCollection services,
			IAssemblyInformationProvider assemblyInformationProvider,
			IIOManager ioManager,
			IPostSetupServices postSetupServices)
		{
			ConfigureServices(services, assemblyInformationProvider, ioManager);

			if (postSetupServices == null)
				throw new ArgumentNullException(nameof(postSetupServices));

			// configure configuration
			services.UseStandardConfig<UpdatesConfiguration>(Configuration);
			services.UseStandardConfig<ControlPanelConfiguration>(Configuration);
			services.UseStandardConfig<SwarmConfiguration>(Configuration);
			services.UseStandardConfig<SessionConfiguration>(Configuration);

			// enable options which give us config reloading
			services.AddOptions();

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
						+ ServiceCollectionExtensions.SerilogContextTemplate
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
				postSetupServices.ElasticsearchConfiguration);

			// configure bearer token validation
			services
				.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
				.AddJwtBearer(jwtBearerOptions =>
				{
					// this line isn't actually run until the first request is made
					// at that point tokenFactory will be populated
					jwtBearerOptions.TokenValidationParameters = tokenFactory.ValidationParameters;
					jwtBearerOptions.Events = new JwtBearerEvents
					{
						// Application is our composition root so this monstrosity of a line is okay
						// At least, that's what I tell myself to sleep at night
						OnTokenValidated = ctx => ctx
							.HttpContext
							.RequestServices
							.GetRequiredService<IClaimsInjector>()
							.InjectClaimsIntoContext(
								ctx,
								ctx.HttpContext.RequestAborted),
					};
				});

			// WARNING: STATIC CODE
			// fucking prevents converting 'sub' to M$ bs
			// can't be done in the above lambda, that's too late
			JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

			// add mvc, configure the json serializer settings
			services
				.AddMvc(options =>
				{
					options.EnableEndpointRouting = false;
					options.ReturnHttpNotAcceptable = true;
					options.RespectBrowserAcceptHeader = true;
				})
				.AddNewtonsoftJson(options =>
				{
					options.AllowInputFormatterExceptionMessages = true;
					options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
					options.SerializerSettings.CheckAdditionalContent = true;
					options.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Error;
					options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
					options.SerializerSettings.Converters = new List<JsonConverter>
					{
						new VersionConverter(),
					};
				});

			if (postSetupServices.GeneralConfiguration.HostApiDocumentation)
			{
				string GetDocumentationFilePath(string assemblyLocation) => ioManager.ConcatPath(ioManager.GetDirectoryName(assemblyLocation), String.Concat(ioManager.GetFileNameWithoutExtension(assemblyLocation), ".xml"));
				var assemblyDocumentationPath = GetDocumentationFilePath(typeof(Application).Assembly.Location);
				var apiDocumentationPath = GetDocumentationFilePath(typeof(ApiHeaders).Assembly.Location);
				services.AddSwaggerGen(genOptions => SwaggerConfiguration.Configure(genOptions, assemblyDocumentationPath, apiDocumentationPath));
				services.AddSwaggerGenNewtonsoftSupport();
			}

			// CORS conditionally enabled later
			services.AddCors();

			// Enable managed HTTP clients
			services.AddHttpClient();
			services.AddSingleton<IAbstractHttpClientFactory, AbstractHttpClientFactory>();

			void AddTypedContext<TContext>() where TContext : DatabaseContext
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

			// configure security services
			services.AddScoped<IAuthenticationContextFactory, AuthenticationContextFactory>();
			services.AddScoped<IClaimsInjector, ClaimsInjector>();
			services.AddSingleton<IOAuthProviders, OAuthProviders>();
			services.AddSingleton<IIdentityCache, IdentityCache>();
			services.AddSingleton<ICryptographySuite, CryptographySuite>();
			services.AddSingleton<ITokenFactory, TokenFactory>();
			services.AddSingleton<IPasswordHasher<Models.User>, PasswordHasher<Models.User>>();

			// configure platform specific services
			if (postSetupServices.PlatformIdentifier.IsWindows)
			{
				AddWatchdog<WindowsWatchdogFactory>(services, postSetupServices);
				services.AddSingleton<ISystemIdentityFactory, WindowsSystemIdentityFactory>();
				services.AddSingleton<ISymlinkFactory, WindowsSymlinkFactory>();
				services.AddSingleton<IByondInstaller, WindowsByondInstaller>();
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
				services.AddSingleton<ISymlinkFactory, PosixSymlinkFactory>();
				services.AddSingleton<IByondInstaller, PosixByondInstaller>();
				services.AddSingleton<IPostWriteHandler, PosixPostWriteHandler>();

				services.AddSingleton<IProcessFeatures, PosixProcessFeatures>();

				// PosixProcessFeatures also needs a IProcessExecutor for gcore
				services.AddSingleton(x => new Lazy<IProcessExecutor>(() => x.GetRequiredService<IProcessExecutor>(), true));
				services.AddSingleton<INetworkPromptReaper, PosixNetworkPromptReaper>();

				services.AddSingleton<IHostedService, PosixSignalHandler>();
			}

			// configure file transfer services
			services.AddSingleton<FileTransferService>();
			services.AddSingleton<IFileTransferStreamHandler>(x => x.GetRequiredService<FileTransferService>());
			services.AddSingleton<IFileTransferTicketProvider>(x => x.GetRequiredService<FileTransferService>());
			services.AddTransient<IActionResultExecutor<LimitedFileStreamResult>, LimitedFileStreamResultExecutor>();

			// configure swarm service
			services.AddSingleton<SwarmService>();
			services.AddSingleton<ISwarmService>(x => x.GetRequiredService<SwarmService>());
			services.AddSingleton<ISwarmOperations>(x => x.GetRequiredService<SwarmService>());
			services.AddSingleton<ISwarmServiceController>(x => x.GetRequiredService<SwarmService>());

			// configure component services
			services.AddScoped<IPortAllocator, PortAllocator>();
			services.AddSingleton<IInstanceFactory, InstanceFactory>();
			services.AddSingleton<IGitRemoteFeaturesFactory, GitRemoteFeaturesFactory>();
			services.AddSingleton<ILibGit2RepositoryFactory, LibGit2RepositoryFactory>();
			services.AddSingleton<ILibGit2Commands, LibGit2Commands>();
			services.AddSingleton<IRemoteDeploymentManagerFactory, RemoteDeploymentManagerFactory>();
			services.AddSingleton<IProviderFactory, ProviderFactory>();
			services.AddSingleton<IChatManagerFactory, ChatManagerFactory>();
			services.AddSingleton<IServerUpdater, ServerUpdater>();
			services.AddSingleton<IServerUpdateInitiator, ServerUpdateInitiator>();

			// configure misc services
			services.AddSingleton<IProcessExecutor, ProcessExecutor>();
			services.AddSingleton<ISynchronousIOManager, SynchronousIOManager>();
			services.AddSingleton<IFileDownloader, FileDownloader>();
			services.AddSingleton<IServerPortProvider, ServerPortProivder>();
			services.AddSingleton<ITopicClientFactory, TopicClientFactory>();
			services.AddSingleton<IGitHubClientFactory, GitHubClientFactory>();

			// configure root services
			services.AddSingleton<IJobManager, JobManager>();

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
		/// <param name="instanceManager">The <see cref="IInstanceManager"/>.</param>
		/// <param name="serverPortProvider">The <see cref="IServerPortProvider"/>.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/>.</param>
		/// <param name="controlPanelConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="ControlPanelConfiguration"/> to use.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="GeneralConfiguration"/> to use.</param>
		/// <param name="logger">The <see cref="Microsoft.Extensions.Logging.ILogger"/> for the <see cref="Application"/>.</param>
		public void Configure(
			IApplicationBuilder applicationBuilder,
			IServerControl serverControl,
			ITokenFactory tokenFactory,
			IInstanceManager instanceManager,
			IServerPortProvider serverPortProvider,
			IAssemblyInformationProvider assemblyInformationProvider,
			IOptions<ControlPanelConfiguration> controlPanelConfigurationOptions,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			ILogger<Application> logger)
		{
			if (applicationBuilder == null)
				throw new ArgumentNullException(nameof(applicationBuilder));
			if (serverControl == null)
				throw new ArgumentNullException(nameof(serverControl));

			this.tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));

			if (instanceManager == null)
				throw new ArgumentNullException(nameof(instanceManager));
			if (serverPortProvider == null)
				throw new ArgumentNullException(nameof(serverPortProvider));
			if (assemblyInformationProvider == null)
				throw new ArgumentNullException(nameof(assemblyInformationProvider));

			var controlPanelConfiguration = controlPanelConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(controlPanelConfigurationOptions));
			var generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));

			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			logger.LogDebug("Content Root: {contentRoot}", hostingEnvironment.ContentRootPath);
			logger.LogTrace("Web Root: {webRoot}", hostingEnvironment.WebRootPath);

			// attempt to restart the server if the configuration changes
			if (serverControl.WatchdogPresent)
				ChangeToken.OnChange(Configuration.GetReloadToken, () =>
				{
					logger.LogInformation("Configuration change detected");
					serverControl.Restart();
				});

			// setup the HTTP request pipeline
			// Final point where we wrap exceptions in a 500 (ErrorMessage) response
			applicationBuilder.UseServerErrorHandling();

			// Add the X-Powered-By response header
			applicationBuilder.UseServerBranding(assemblyInformationProvider);

			// suppress OperationCancelledExceptions, they are just aborted HTTP requests
			applicationBuilder.UseCancelledRequestSuppression();

			// 503 requests made while the application is starting
			applicationBuilder.UseAsyncInitialization(instanceManager.Ready.WithToken);

			if (generalConfiguration.HostApiDocumentation)
			{
				applicationBuilder.UseSwagger();
				applicationBuilder.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TGS API"));
				logger.LogTrace("Swagger API generation enabled");
			}

			// Set up CORS based on configuration if necessary
			Action<CorsPolicyBuilder> corsBuilder = null;
			if (controlPanelConfiguration.AllowAnyOrigin)
			{
				logger.LogTrace("Access-Control-Allow-Origin: *");
				corsBuilder = builder => builder.AllowAnyOrigin();
			}
			else if (controlPanelConfiguration.AllowedOrigins?.Count > 0)
			{
				logger.LogTrace("Access-Control-Allow-Origin: {allowedOrigins}", String.Join(',', controlPanelConfiguration.AllowedOrigins));
				corsBuilder = builder => builder.WithOrigins(controlPanelConfiguration.AllowedOrigins.ToArray());
			}

			var originalBuilder = corsBuilder;
			corsBuilder = builder =>
			{
				builder
					.AllowAnyHeader()
					.AllowAnyMethod()
					.SetPreflightMaxAge(TimeSpan.FromDays(1));
				originalBuilder?.Invoke(builder);
			};
			applicationBuilder.UseCors(corsBuilder);

			// spa loading if necessary
			if (controlPanelConfiguration.Enable)
			{
				logger.LogInformation("Web control panel enabled.");
				applicationBuilder.UseFileServer(new FileServerOptions
				{
					RequestPath = ControlPanelController.ControlPanelRoute,
					EnableDefaultFiles = true,
					EnableDirectoryBrowsing = false,
				});
			}
			else
				logger.LogTrace("Web control panel disabled!");

			// Do not cache a single thing beyond this point, it's all API
			applicationBuilder.UseDisabledClientCache();

			// authenticate JWT tokens using our security pipeline if present, returns 401 if bad
			applicationBuilder.UseAuthentication();

			// suppress and log database exceptions
			applicationBuilder.UseDbConflictHandling();

			// majority of handling is done in the controllers
			applicationBuilder.UseMvc();

			// 404 anything that gets this far
			// End of request pipeline setup
			logger.LogTrace("Configuration version: {configVersion}", GeneralConfiguration.CurrentConfigVersion);
			logger.LogTrace("DMAPI Interop version: {interopVersion}", DMApiConstants.InteropVersion);
			if (controlPanelConfiguration.Enable)
				logger.LogTrace("Web control panel version: {webCPVersion}", MasterVersionsAttribute.Instance.RawControlPanelVersion);

			logger.LogDebug("Starting hosting on port {httpApiPort}...", serverPortProvider.HttpApiPort);
		}

		/// <inheritdoc />
		protected override void ConfigureHostedService(IServiceCollection services)
			=> services.AddSingleton<IHostedService>(x => x.GetRequiredService<InstanceManager>());
	}
}
