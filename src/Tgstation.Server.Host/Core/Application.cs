using Cyberboss.AspNetCore.AsyncInitializer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
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
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Chat.Providers;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Components.Interop.Converters;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Properties;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Security.OAuth;
using Tgstation.Server.Host.Setup;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Sets up dependency injection.
	/// </summary>
#pragma warning disable CA1506
	sealed class Application : SetupApplication
	{
		/// <summary>
		/// Route to the web control panel.
		/// </summary>
		public const string ControlPanelRoute = "/app";

		/// <summary>
		/// The <see cref="IWebHostEnvironment"/> for the <see cref="Application"/>.
		/// </summary>
		readonly IWebHostEnvironment hostingEnvironment;

		/// <summary>
		/// The <see cref="ITokenFactory"/> for the <see cref="Application"/>
		/// </summary>
		ITokenFactory tokenFactory;

		/// <summary>
		/// Construct an <see cref="Application"/>
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
		/// Create the default <see cref="IServerFactory"/>.
		/// </summary>
		/// <returns>A new <see cref="IServerFactory"/> with the default settings.</returns>
		public static IServerFactory CreateDefaultServerFactory()
			=> new ServerFactory(
				AssemblyInformationProvider,
				IOManager);

		/// <summary>
		/// Configure the <see cref="Application"/>'s services.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
		/// <param name="postSetupServices">The <see cref="IPostSetupServices"/> needed for configuration.</param>
		public void ConfigureServices(IServiceCollection services, IPostSetupServices postSetupServices)
		{
			ConfigureServices(services);

			if (postSetupServices == null)
				throw new ArgumentNullException(nameof(postSetupServices));

			// configure configuration
			services.UseStandardConfig<UpdatesConfiguration>(Configuration);
			services.UseStandardConfig<ControlPanelConfiguration>(Configuration);

			// enable options which give us config reloading
			services.AddOptions();

			// Set the timeout for IHostedService.StopAsync
			services.Configure<HostOptions>(
				opts => opts.ShutdownTimeout = TimeSpan.FromMilliseconds(postSetupServices.GeneralConfiguration.RestartTimeout));

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
						IOManager,
						AssemblyInformationProvider,
						postSetupServices.PlatformIdentifier);

					var logEventLevel = ConvertSeriLogLevel(postSetupServices.FileLoggingConfiguration.LogLevel);

					var formatter = new MessageTemplateTextFormatter(
						"{Timestamp:o} "
						+ ServiceCollectionExtensions.SerilogContextTemplate
						+ "): [{Level:u3}] {SourceContext:l}: {Message} ({EventId:x8}){NewLine}{Exception}",
						null);

					logPath = IOManager.ConcatPath(logPath, "tgs-.log");
					var rollingFileConfig = sinkConfig.File(
						formatter,
						logPath,
						logEventLevel ?? LogEventLevel.Verbose,
						50 * 1024 * 1024, // 50MB max size
						flushToDiskInterval: TimeSpan.FromSeconds(2),
						rollingInterval: RollingInterval.Day,
						rollOnFileSizeLimit: true);
				});

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
								ctx.HttpContext.RequestAborted)
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
					options.SerializerSettings.Converters = new[] { new VersionConverter() };
				});

			if (postSetupServices.GeneralConfiguration.HostApiDocumentation)
			{
				static string GetDocumentationFilePath(string assemblyLocation) => IOManager.ConcatPath(IOManager.GetDirectoryName(assemblyLocation), String.Concat(IOManager.GetFileNameWithoutExtension(assemblyLocation), ".xml"));
				var assemblyDocumentationPath = GetDocumentationFilePath(typeof(Application).Assembly.Location);
				var apiDocumentationPath = GetDocumentationFilePath(typeof(ApiHeaders).Assembly.Location);
				services.AddSwaggerGen(genOptions => SwaggerConfiguration.Configure(genOptions, assemblyDocumentationPath, apiDocumentationPath));
				services.AddSwaggerGenNewtonsoftSupport();
			}

			// enable browser detection
			services.AddDetectionCore().AddBrowser();

			// CORS conditionally enabled later
			services.AddCors();

			// Enable managed HTTP clients
			services.AddHttpClient();

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
			services.AddScoped<IOAuthProviders, OAuthProviders>();
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
			}

			// configure misc services
			services.AddSingleton<ISynchronousIOManager, SynchronousIOManager>();
			services.AddSingleton<IGitHubClientFactory, GitHubClientFactory>();
			services.AddSingleton<IProcessExecutor, ProcessExecutor>();
			services.AddSingleton<IServerPortProvider, ServerPortProivder>();
			services.AddSingleton<ITopicClientFactory, TopicClientFactory>();
			services.AddScoped<IPortAllocator, PortAllocator>();

			// configure component services
			services.AddSingleton<ILibGit2RepositoryFactory, LibGit2RepositoryFactory>();
			services.AddSingleton<ILibGit2Commands, LibGit2Commands>();
			services.AddSingleton<IProviderFactory, ProviderFactory>();
			services.AddSingleton<IChatManagerFactory, ChatManagerFactory>();
			services.AddSingleton<IInstanceFactory, InstanceFactory>();

			// configure root services
			services.AddSingleton<IJobManager, JobManager>();

			services.AddSingleton<InstanceManager>();
			services.AddSingleton<IBridgeDispatcher>(x => x.GetRequiredService<InstanceManager>());
			services.AddSingleton(x => new Lazy<IInstanceCoreProvider>(() => x.GetRequiredService<InstanceManager>()));
			services.AddSingleton<IInstanceManager>(x => x.GetRequiredService<InstanceManager>());
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

		/// <inheritdoc />
		protected override void ConfigureHostedService(IServiceCollection services)
			=> services.AddSingleton<IHostedService>(x => x.GetRequiredService<InstanceManager>());

		/// <summary>
		/// Configure the <see cref="Application"/>
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure</param>
		/// <param name="serverControl">The <see cref="IServerControl"/> for the <see cref="Application"/></param>
		/// <param name="tokenFactory">The value of <see cref="tokenFactory"/></param>
		/// <param name="instanceManager">The <see cref="IInstanceManager"/>.</param>
		/// <param name="controlPanelConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="ControlPanelConfiguration"/> to use</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="GeneralConfiguration"/> to use</param>
		/// <param name="logger">The <see cref="Microsoft.Extensions.Logging.ILogger"/> for the <see cref="Application"/></param>
		public void Configure(
			IApplicationBuilder applicationBuilder,
			IServerControl serverControl,
			ITokenFactory tokenFactory,
			IInstanceManager instanceManager,
			IOptions<ControlPanelConfiguration> controlPanelConfigurationOptions,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			ILogger<Application> logger)
		{
			if (applicationBuilder == null)
				throw new ArgumentNullException(nameof(applicationBuilder));
			if (serverControl == null)
				throw new ArgumentNullException(nameof(serverControl));

			this.tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));

			var controlPanelConfiguration = controlPanelConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(controlPanelConfigurationOptions));
			var generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));

			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			logger.LogDebug("Content Root: {0}", hostingEnvironment.ContentRootPath);
			logger.LogTrace("Web Root: {0}", hostingEnvironment.WebRootPath);

			if (generalConfiguration.MinimumPasswordLength > Limits.MaximumStringLength)
			{
				logger.LogCritical("Configured minimum password length ({0}) is greater than the maximum database string length ({1})!");
				serverControl.Die(new InvalidOperationException("Minimum password length greater than database limit!"));
				return;
			}

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
			applicationBuilder.UseServerBranding(AssemblyInformationProvider);

			// 503 requests made while the application is starting
			applicationBuilder.UseAsyncInitialization(async (cancellationToken) =>
			{
				var tcs = new TaskCompletionSource<object>();
				using (cancellationToken.Register(() => tcs.SetCanceled()))
					await Task.WhenAny(tcs.Task, instanceManager.Ready).ConfigureAwait(false);
			});

			// suppress OperationCancelledExceptions, they are just aborted HTTP requests
			applicationBuilder.UseCancelledRequestSuppression();

			if (generalConfiguration.HostApiDocumentation)
			{
				applicationBuilder.UseSwagger();
				applicationBuilder.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TGS API V4"));
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
				logger.LogTrace("Access-Control-Allow-Origin: ", String.Join(',', controlPanelConfiguration.AllowedOrigins));
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
				logger.LogWarning("Web control panel enabled. This is a highly WIP feature!");
				applicationBuilder.UseFileServer(new FileServerOptions
				{
					RequestPath = ControlPanelRoute,
					EnableDefaultFiles = true,
					EnableDirectoryBrowsing = false
				});
			}
			else
				logger.LogDebug("Web control panel disabled!");

			// authenticate JWT tokens using our security pipeline if present, returns 401 if bad
			applicationBuilder.UseAuthentication();

			// suppress and log database exceptions
			applicationBuilder.UseDbConflictHandling();

			// majority of handling is done in the controllers
			applicationBuilder.UseMvc();

			// 404 anything that gets this far
			// End of request pipeline setup
			var masterVersionsAttribute = MasterVersionsAttribute.Instance;
			logger.LogTrace("Configuration version: {0}", masterVersionsAttribute.RawConfigurationVersion);
			logger.LogTrace("DMAPI version: {0}", masterVersionsAttribute.RawDMApiVersion);
			logger.LogTrace("Web control panel version: {0}", masterVersionsAttribute.RawControlPanelVersion);

			logger.LogDebug("Starting hosting...");
		}
	}
}
