using Byond.TopicSender;
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
using Newtonsoft.Json.Converters;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Sets up dependency injection.
	/// </summary>
#pragma warning disable CA1506
	sealed class Application
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="Application"/>.
		/// </summary>
		static readonly IIOManager IOManager = new DefaultIOManager();

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="Application"/>.
		/// </summary>
		static readonly IAssemblyInformationProvider AssemblyInformationProvider = new AssemblyInformationProvider();

		/// <summary>
		/// The <see cref="IConfiguration"/> for the <see cref="Application"/>
		/// </summary>
		readonly IConfiguration configuration;

		/// <summary>
		/// The <see cref="IWebHostEnvironment"/> for the <see cref="Application"/>
		/// </summary>
		readonly IWebHostEnvironment hostingEnvironment;

		/// <summary>
		/// The <see cref="ITokenFactory"/> for the <see cref="Application"/>
		/// </summary>
		ITokenFactory tokenFactory;

		/// <summary>
		/// Construct an <see cref="Application"/>
		/// </summary>
		/// <param name="configuration">The value of <see cref="configuration"/></param>
		/// <param name="hostingEnvironment">The value of <see cref="hostingEnvironment"/></param>
		public Application(
			IConfiguration configuration,
			IWebHostEnvironment hostingEnvironment)
		{
			this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			this.hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
		}

		/// <summary>
		/// Create the default <see cref="IServerFactory"/>.
		/// </summary>
		/// <returns>A new <see cref="IServerFactory"/> with the default settings.</returns>
		public static IServerFactory CreateDefaultServerFactory()
			=> new ServerFactory<Application>(
				AssemblyInformationProvider,
				IOManager);

		/// <summary>
		/// Configure dependency injected services
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to configure</param>
		public void ConfigureServices(IServiceCollection services)
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));

			// configure configuration
			services.UseStandardConfig<UpdatesConfiguration>(configuration);
			services.UseStandardConfig<DatabaseConfiguration>(configuration);
			services.UseStandardConfig<GeneralConfiguration>(configuration);
			services.UseStandardConfig<FileLoggingConfiguration>(configuration);
			services.UseStandardConfig<ControlPanelConfiguration>(configuration);

			// enable options which give us config reloading
			services.AddOptions();

			// this is needful for the setup wizard
			services.AddLogging();

			// other stuff needed for for setup wizard and configuration
			services.AddSingleton(IOManager);
			services.AddSingleton(AssemblyInformationProvider);
			services.AddSingleton<IConsole, IO.Console>();
			services.AddSingleton<IDatabaseConnectionFactory, DatabaseConnectionFactory>();
			services.AddSingleton<ISetupWizard, SetupWizard>();
			services.AddSingleton<IPlatformIdentifier, PlatformIdentifier>();
			services.AddSingleton<IAsyncDelayer, AsyncDelayer>();

			GeneralConfiguration generalConfiguration;
			DatabaseConfiguration databaseConfiguration;
			FileLoggingConfiguration fileLoggingConfiguration;
			ControlPanelConfiguration controlPanelConfiguration;
			IPlatformIdentifier platformIdentifier;

			// temporarily build the service provider in it's current state
			// do it here so we can run the setup wizard if necessary
			// also allows us to get some options and other services we need for continued configuration
#pragma warning disable ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'
			using (var provider = services.BuildServiceProvider())
#pragma warning restore ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'
			{
				// run the wizard if necessary
				var setupWizard = provider.GetRequiredService<ISetupWizard>();
				var applicationLifetime = provider.GetRequiredService<IHostApplicationLifetime>();
				var setupWizardRan = setupWizard.CheckRunWizard(applicationLifetime.ApplicationStopping).GetAwaiter().GetResult();

				// load the configuration options we need
				var generalOptions = provider.GetRequiredService<IOptions<GeneralConfiguration>>();
				generalConfiguration = generalOptions.Value;

				// unless this is set, in which case, we leave
				if (setupWizardRan && generalConfiguration.SetupWizardMode == SetupWizardMode.Only)
					throw new OperationCanceledException("Exiting due to SetupWizardMode configuration!"); // we don't inject a logger in the constuctor to log this because it's not yet configured

				var dbOptions = provider.GetRequiredService<IOptions<DatabaseConfiguration>>();
				databaseConfiguration = dbOptions.Value;

				var loggingOptions = provider.GetRequiredService<IOptions<FileLoggingConfiguration>>();
				fileLoggingConfiguration = loggingOptions.Value;

				var controlPanelOptions = provider.GetRequiredService<IOptions<ControlPanelConfiguration>>();
				controlPanelConfiguration = controlPanelOptions.Value;

				platformIdentifier = provider.GetRequiredService<IPlatformIdentifier>();
			}

			// setup file logging via serilog
			if (!fileLoggingConfiguration.Disable)
				services.AddLogging(builder =>
				{
					// common app data is C:/ProgramData on windows, else /usr/share
					var logPath = !String.IsNullOrEmpty(fileLoggingConfiguration.Directory)
					? fileLoggingConfiguration.Directory
					: IOManager.ConcatPath(
						Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
						"tgstation-server",
						"Logs");

					logPath = IOManager.ConcatPath(logPath, "tgs-{Date}.log");

					static LogEventLevel? ConvertLogLevel(LogLevel logLevel) =>
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

					var logEventLevel = ConvertLogLevel(fileLoggingConfiguration.LogLevel);
					var microsoftEventLevel = ConvertLogLevel(fileLoggingConfiguration.MicrosoftLogLevel);

					var formatter = new MessageTemplateTextFormatter(
						"{Timestamp:o} {RequestId,13} [{Level:u3}] {SourceContext:l}: {Message} ({EventId:x8}){NewLine}{Exception}",
						null);

					var configuration = new LoggerConfiguration()
						.Enrich
						.FromLogContext()
						.WriteTo
						.Async(
							w => w.RollingFile(
								formatter,
								logPath,
								shared: true,
								flushToDiskInterval: TimeSpan.FromSeconds(2)));

					if (logEventLevel.HasValue)
						configuration.MinimumLevel.Is(logEventLevel.Value);

					if (microsoftEventLevel.HasValue)
						configuration.MinimumLevel.Override("Microsoft", microsoftEventLevel.Value);

					builder.AddSerilog(configuration.CreateLogger(), true);
				});

			// configure bearer token validation
			services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(jwtBearerOptions =>
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

			// fucking prevents converting 'sub' to M$ bs
			// can't be done in the above lambda, that's too late
			JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

			// add mvc, configure the json serializer settings
			services
				.AddMvc(options =>
				{
					options.EnableEndpointRouting = false;
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

			if (hostingEnvironment.IsDevelopment())
			{
				string GetDocumentationFilePath(string assemblyLocation) => IOManager.ConcatPath(IOManager.GetDirectoryName(assemblyLocation), String.Concat(IOManager.GetFileNameWithoutExtension(assemblyLocation), ".xml"));
				var assemblyDocumentationPath = GetDocumentationFilePath(typeof(Application).Assembly.Location);
				var apiDocumentationPath = GetDocumentationFilePath(typeof(ApiHeaders).Assembly.Location);
				services.AddSwaggerGen(genOptions => SwaggerConfiguration.Configure(genOptions, assemblyDocumentationPath, apiDocumentationPath));
				services.AddSwaggerGenNewtonsoftSupport();
			}

			// enable browser detection
			services.AddDetectionCore().AddBrowser();

			// CORS conditionally enabled later
			services.AddCors();

			void AddTypedContext<TContext>() where TContext : DatabaseContext<TContext>
			{
				services.AddDbContext<TContext>(builder =>
				{
					if (hostingEnvironment.IsDevelopment())
						builder.EnableSensitiveDataLogging();
				});
				services.AddScoped<IDatabaseContext>(x => x.GetRequiredService<TContext>());
			}

			// add the correct database context type
			var dbType = databaseConfiguration.DatabaseType;
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
				default:
					throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid {0}: {1}!", nameof(DatabaseType), dbType));
			}

			// configure other database services
			services.AddSingleton<IDatabaseContextFactory, DatabaseContextFactory>();
			services.AddSingleton<IDatabaseSeeder, DatabaseSeeder>();

			// configure security services
			services.AddScoped<IAuthenticationContextFactory, AuthenticationContextFactory>();
			services.AddScoped<IClaimsInjector, ClaimsInjector>();
			services.AddSingleton<IIdentityCache, IdentityCache>();
			services.AddSingleton<ICryptographySuite, CryptographySuite>();
			services.AddSingleton<ITokenFactory, TokenFactory>();
			services.AddSingleton<IPasswordHasher<Models.User>, PasswordHasher<Models.User>>();

			// configure platform specific services
			if (platformIdentifier.IsWindows)
			{
				if (generalConfiguration.UseBasicWatchdogOnWindows)
					services.AddSingleton<IWatchdogFactory, WatchdogFactory>();
				else
					services.AddSingleton<IWatchdogFactory, WindowsWatchdogFactory>();

				services.AddSingleton<ISystemIdentityFactory, WindowsSystemIdentityFactory>();
				services.AddSingleton<ISymlinkFactory, WindowsSymlinkFactory>();
				services.AddSingleton<IByondInstaller, WindowsByondInstaller>();
				services.AddSingleton<IPostWriteHandler, WindowsPostWriteHandler>();

				services.AddSingleton<WindowsNetworkPromptReaper>();
				services.AddSingleton<INetworkPromptReaper>(x => x.GetRequiredService<WindowsNetworkPromptReaper>());
				services.AddSingleton<IHostedService>(x => x.GetRequiredService<WindowsNetworkPromptReaper>());
			}
			else
			{
				services.AddSingleton<IWatchdogFactory, WatchdogFactory>();
				services.AddSingleton<ISystemIdentityFactory, PosixSystemIdentityFactory>();
				services.AddSingleton<ISymlinkFactory, PosixSymlinkFactory>();
				services.AddSingleton<IByondInstaller, PosixByondInstaller>();
				services.AddSingleton<IPostWriteHandler, PosixPostWriteHandler>();
				services.AddSingleton<INetworkPromptReaper, PosixNetworkPromptReaper>();
			}

			// configure misc services
			services.AddSingleton<ISynchronousIOManager, SynchronousIOManager>();
			services.AddSingleton<IGitHubClientFactory, GitHubClientFactory>();
			services.AddSingleton<IProcessExecutor, ProcessExecutor>();
			services.AddSingleton<IByondTopicSender>(new ByondTopicSender
			{
				ReceiveTimeout = generalConfiguration.ByondTopicTimeout,
				SendTimeout = generalConfiguration.ByondTopicTimeout
			});

			// configure component services
			services.AddSingleton<IRepositoryFactory, RepositoryFactory>();
			services.AddSingleton<IProviderFactory, ProviderFactory>();
			services.AddSingleton<IChatFactory, ChatFactory>();
			services.AddSingleton<IInstanceFactory, InstanceFactory>();

			// configure root services
			services.AddSingleton<InstanceManager>();
			services.AddSingleton<IInstanceManager>(x => x.GetRequiredService<InstanceManager>());
			services.AddSingleton<IHostedService>(x => x.GetRequiredService<InstanceManager>());

			services.AddSingleton<IJobManager, JobManager>();
		}

		/// <summary>
		/// Configure the <see cref="Application"/>
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure</param>
		/// <param name="serverControl">The <see cref="IServerControl"/> for the <see cref="Application"/></param>
		/// <param name="tokenFactory">The value of <see cref="tokenFactory"/></param>
		/// <param name="controlPanelConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="ControlPanelConfiguration"/> to use</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="GeneralConfiguration"/> to use</param>
		/// <param name="logger">The <see cref="Microsoft.Extensions.Logging.ILogger"/> for the <see cref="Application"/></param>
		public void Configure(
			IApplicationBuilder applicationBuilder,
			IServerControl serverControl,
			ITokenFactory tokenFactory,
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
				ChangeToken.OnChange(configuration.GetReloadToken, () => serverControl.Restart());

			// setup the HTTP request pipeline
			// Final point where we wrap exceptions in a 500 (ErrorMessage) response
			applicationBuilder.UseServerErrorHandling();

			// 503 requests made while the application is starting
			applicationBuilder.UseAsyncInitialization<IHostApplicationLifetime>(async (applicationLifetime, cancellationToken) =>
			{
				var tcs = new TaskCompletionSource<object>();
				using (cancellationToken.Register(() => tcs.SetCanceled()))
				using (applicationLifetime.ApplicationStarted.Register(() => tcs.SetResult(null)))
					await tcs.Task.ConfigureAwait(false);
			});

			// should anything after this throw an exception, catch it and display a detailed html page
			if (hostingEnvironment.IsDevelopment())
				applicationBuilder.UseDeveloperExceptionPage(); // it is not worth it to limit this, you should only ever get it if you're an authorized user

			// suppress OperationCancelledExceptions, they are just aborted HTTP requests
			applicationBuilder.UseCancelledRequestSuppression();

			if (hostingEnvironment.IsDevelopment())
			{
				applicationBuilder.UseSwagger();
				applicationBuilder.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TGS API V4"));
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
				builder.AllowAnyHeader().AllowAnyMethod();
				originalBuilder?.Invoke(builder);
			};
			applicationBuilder.UseCors(corsBuilder);

			// spa loading if necessary
			if (controlPanelConfiguration.Enable)
			{
				logger.LogWarning("Web control panel enabled. This is a highly WIP feature!");
				applicationBuilder.UseStaticFiles();
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
		}
	}
}