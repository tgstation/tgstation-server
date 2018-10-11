using Byond.TopicSender;
using Cyberboss.AspNetCore.AsyncInitializer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class Application : IApplication
	{
		/// <inheritdoc />
		public string VersionPrefix => "tgstation-server";

		/// <inheritdoc />
		public Version Version { get; }

		/// <inheritdoc />
		public string VersionString { get; }

		/// <summary>
		/// The <see cref="IConfiguration"/> for the <see cref="Application"/>
		/// </summary>
		readonly IConfiguration configuration;

		/// <summary>
		/// The <see cref="Microsoft.AspNetCore.Hosting.IHostingEnvironment"/> for the <see cref="Application"/>
		/// </summary>
		readonly Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment;

		/// <summary>
		/// The <see cref="TaskCompletionSource{TResult}"/> used for determining when the <see cref="Application"/> is <see cref="Ready(Exception)"/>
		/// </summary>
		readonly TaskCompletionSource<object> startupTcs;

		/// <summary>
		/// Construct an <see cref="Application"/>
		/// </summary>
		/// <param name="configuration">The value of <see cref="configuration"/></param>
		/// <param name="hostingEnvironment">The value of <see cref="hostingEnvironment"/></param>
		public Application(IConfiguration configuration, Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment)
		{
			this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			this.hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));

			startupTcs = new TaskCompletionSource<object>();

			Version = Assembly.GetExecutingAssembly().GetName().Version;
			VersionString = String.Format(CultureInfo.InvariantCulture, "{0} v{1}", VersionPrefix, Version);
		}

		/// <summary>
		/// Configure dependency injected services
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to configure</param>
		public void ConfigureServices(IServiceCollection services)
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));

			//needful
			services.AddSingleton<IApplication>(this);

			//configure configuration
			services.Configure<UpdatesConfiguration>(configuration.GetSection(UpdatesConfiguration.Section));
			services.Configure<DatabaseConfiguration>(configuration.GetSection(DatabaseConfiguration.Section));
			services.Configure<GeneralConfiguration>(configuration.GetSection(GeneralConfiguration.Section));
			services.Configure<FileLoggingConfiguration>(configuration.GetSection(FileLoggingConfiguration.Section));

			//enable options which give us config reloading
			services.AddOptions();
					
			//setup stuff for setup wizard
			services.AddSingleton<IIOManager, DefaultIOManager>();
			services.AddSingleton<IConsole, IO.Console>();
			services.AddSingleton<IDBConnectionFactory, DBConnectionFactory>();
			services.AddSingleton<ISetupWizard, SetupWizard>();

			//needed here for JWT configuration
			//we use a manually instatiated token factory to prevent it from regenerating the signing key after we configure it
			services.AddSingleton<ITokenFactory>(new TokenFactory());

			GeneralConfiguration generalConfiguration;
			DatabaseConfiguration databaseConfiguration;
			FileLoggingConfiguration fileLoggingConfiguration;
			ITokenFactory tokenFactory;
			IIOManager ioManager;

			//temporarily build the service provider in it's current state
			//do it here so we can run the setup wizard if necessary
			//also allows us to get some options and other services we need for continued configuration
			using (var provider = services.BuildServiceProvider())
			{
				//run the wizard if necessary
				var setupWizard = provider.GetRequiredService<ISetupWizard>();
				var applicationLifetime = provider.GetRequiredService<Microsoft.AspNetCore.Hosting.IApplicationLifetime>();
				var setupWizardRan = setupWizard.CheckRunWizard(applicationLifetime.ApplicationStopping).GetAwaiter().GetResult();

				//load the configuration options we need
				var generalOptions = provider.GetRequiredService<IOptions<GeneralConfiguration>>();
				generalConfiguration = generalOptions.Value;

				//unless this is set, in which case, we leave
				if (setupWizardRan && generalConfiguration.SetupWizardMode == SetupWizardMode.Only)
					//we don't inject a logger in the constuctor to log this because it's not yet configured
					throw new OperationCanceledException("Exiting due to SetupWizardMode configuration!");

				var dbOptions = provider.GetRequiredService<IOptions<DatabaseConfiguration>>();
				databaseConfiguration = dbOptions.Value;

				var loggingOptions = provider.GetRequiredService<IOptions<FileLoggingConfiguration>>();
				fileLoggingConfiguration = loggingOptions.Value;

				tokenFactory = provider.GetRequiredService<ITokenFactory>();
				ioManager = provider.GetRequiredService<IIOManager>();
			}

			//setup file logging via serilog
			if (!fileLoggingConfiguration.Disable)
				services.AddLogging(builder =>
				{
					//common app data is C:/ProgramData on windows, else /usr/shar
					var logPath = !String.IsNullOrEmpty(fileLoggingConfiguration.Directory) ? fileLoggingConfiguration.Directory : ioManager.ConcatPath(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), VersionPrefix, "Logs");

					logPath = ioManager.ConcatPath(logPath, "tgs-{Date}.log");

					LogEventLevel? ConvertLogLevel(LogLevel logLevel)
					{
						switch (logLevel)
						{
							case LogLevel.Critical:
								return LogEventLevel.Fatal;
							case LogLevel.Debug:
								return LogEventLevel.Debug;
							case LogLevel.Error:
								return LogEventLevel.Error;
							case LogLevel.Information:
								return LogEventLevel.Information;
							case LogLevel.Trace:
								return LogEventLevel.Verbose;
							case LogLevel.Warning:
								return LogEventLevel.Warning;
							case LogLevel.None:
								return null;
							default:
								throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid log level {0}", logLevel));
						}
					};

					var logEventLevel = ConvertLogLevel(fileLoggingConfiguration.LogLevel);
					var microsoftEventLevel = ConvertLogLevel(fileLoggingConfiguration.MicrosoftLogLevel);

					var formatter = new MessageTemplateTextFormatter("{Timestamp:o} {RequestId,13} [{Level:u3}] {SourceContext:l}: {Message} ({EventId:x8}){NewLine}{Exception}", null);

					var configuration = new LoggerConfiguration()
					.Enrich.FromLogContext()
					.WriteTo.Async(w => w.RollingFile(formatter, logPath, shared: true, flushToDiskInterval: TimeSpan.FromSeconds(2)));

					if (logEventLevel.HasValue)
						configuration.MinimumLevel.Is(logEventLevel.Value);

					if (microsoftEventLevel.HasValue)
						configuration.MinimumLevel.Override("Microsoft", microsoftEventLevel.Value);

					builder.AddSerilog(configuration.CreateLogger(), true);
				});

			//configure bearer token validation
			services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(jwtBearerOptions =>
			{
				jwtBearerOptions.TokenValidationParameters = tokenFactory.ValidationParameters;
				jwtBearerOptions.Events = new JwtBearerEvents
				{
					//Application is our composition root so this monstrosity of a line is okay
					OnTokenValidated = ctx => ctx.HttpContext.RequestServices.GetRequiredService<IClaimsInjector>().InjectClaimsIntoContext(ctx, ctx.HttpContext.RequestAborted)
				};
			});
			//fucking converts 'sub' to M$ bs
			//can't be done in the above lambda, that's too late
			JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

			//add mvc, configure the json serializer settings
			services.AddMvc().AddJsonOptions(options =>
			{
				options.AllowInputFormatterExceptionMessages = true;
				options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
				options.SerializerSettings.CheckAdditionalContent = true;
				options.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Error;
				options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
				options.SerializerSettings.Converters = new[] { new VersionConverter() };
			});

			void AddTypedContext<TContext>() where TContext : DatabaseContext<TContext>
			{
				services.AddDbContext<TContext>(builder =>
				{
					if (hostingEnvironment.IsDevelopment())
						builder.EnableSensitiveDataLogging();
				});
				services.AddScoped<IDatabaseContext>(x => x.GetRequiredService<TContext>());
			}

			//add the correct database context type
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
				default:
					throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid {0}: {1}!", nameof(DatabaseType), dbType));
			}

			//configure other database services
			services.AddSingleton<IDatabaseContextFactory, DatabaseContextFactory>();
			services.AddSingleton<IDatabaseSeeder, DatabaseSeeder>();

			//configure security services
			services.AddScoped<IAuthenticationContextFactory, AuthenticationContextFactory>();
			services.AddScoped<IClaimsInjector, ClaimsInjector>();
			services.AddSingleton<IIdentityCache, IdentityCache>();
			services.AddSingleton<ICryptographySuite, CryptographySuite>();
			services.AddSingleton<IPasswordHasher<Models.User>, PasswordHasher<Models.User>>();

			//configure platform specific services
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
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
				services.AddSingleton<ISystemIdentityFactory, PosixSystemIdentityFactory>();
				services.AddSingleton<ISymlinkFactory, PosixSymlinkFactory>();
				services.AddSingleton<IByondInstaller, PosixByondInstaller>();
				services.AddSingleton<IPostWriteHandler, PosixPostWriteHandler>();
				services.AddSingleton<INetworkPromptReaper, PosixNetworkPromptReaper>();
			}

			//configure misc services
			services.AddSingleton<ISynchronousIOManager, SynchronousIOManager>();
			services.AddSingleton<IGitHubClientFactory, GitHubClientFactory>();
			services.AddSingleton<IProcessExecutor, ProcessExecutor>();
			services.AddSingleton<IByondTopicSender>(new ByondTopicSender
			{
				ReceiveTimeout = generalConfiguration.ByondTopicTimeout,
				SendTimeout = generalConfiguration.ByondTopicTimeout
			});

			//configure component services
			services.AddSingleton<ICredentialsProvider, CredentialsProvider>();
			services.AddSingleton<IProviderFactory, ProviderFactory>();
			services.AddSingleton<IChatFactory, ChatFactory>();
			services.AddSingleton<IWatchdogFactory, WatchdogFactory>();
			services.AddSingleton<IInstanceFactory, InstanceFactory>();

			//configure root services
			services.AddSingleton<InstanceManager>();
			services.AddSingleton<IInstanceManager>(x => x.GetRequiredService<InstanceManager>());
			services.AddSingleton<IHostedService>(x => x.GetRequiredService<InstanceManager>());

			services.AddSingleton<IJobManager, JobManager>();
		}

		/// <summary>
		/// Configure the <see cref="Application"/>
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure</param>
		/// <param name="logger">The <see cref="Microsoft.Extensions.Logging.ILogger"/> for the <see cref="Application"/></param>
		/// <param name="serverControl">The <see cref="IServerControl"/> for the application</param>
		public void Configure(IApplicationBuilder applicationBuilder, ILogger<Application> logger, IServerControl serverControl)
		{
			if (applicationBuilder == null)
				throw new ArgumentNullException(nameof(applicationBuilder));
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (serverControl == null)
				throw new ArgumentNullException(nameof(serverControl));

			logger.LogInformation(VersionString);

			//attempt to restart the server if the configuration changes
			if(serverControl.WatchdogPresent)
				ChangeToken.OnChange(configuration.GetReloadToken, () => serverControl.Restart());

			//now setup the HTTP request pipeline

			//should anything after this throw an exception, catch it and display a detailed html page
			applicationBuilder.UseDeveloperExceptionPage(); //it is not worth it to limit this, you should only ever get it if you're an authorized user
			
			//suppress OperationCancelledExceptions, they are just aborted HTTP requests
			applicationBuilder.UseCancelledRequestSuppression();

			//Do not service requests until Ready is called, this will return 503 until that point
			applicationBuilder.UseAsyncInitialization(async cancellationToken =>
			{
				using (cancellationToken.Register(() => startupTcs.SetCanceled()))
					await startupTcs.Task.ConfigureAwait(false);
			});

			//authenticate JWT tokens using our security pipeline if present, returns 401 if bad
			applicationBuilder.UseAuthentication();

			//suppress and log database exceptions
			applicationBuilder.UseDbConflictHandling();

			//majority of handling is done in the controllers
			applicationBuilder.UseMvc();

			//404 anything that gets this far
		}

		///<inheritdoc />
		public void Ready(Exception initializationError)
		{
			lock (startupTcs)
			{
				if (startupTcs.Task.IsCompleted)
					throw new InvalidOperationException("Ready has already been called!");
				if (initializationError == null)
					startupTcs.SetResult(null);
				else
					startupTcs.SetException(initializationError);
			}
		}
	}
}