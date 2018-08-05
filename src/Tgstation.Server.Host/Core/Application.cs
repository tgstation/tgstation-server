using Byond.TopicSender;
using Cyberboss.AspNetCore.AsyncInitializer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.StaticFiles;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class Application : IApplication
	{
		/// <summary>
		/// Prefix for string version names
		/// </summary>
		public string VersionPrefix => "tgstation-server";

		/// <inheritdoc />
		public string HostingPath => serverAddresses.Addresses.First();

		/// <inheritdoc />
		public Version Version { get; }

		/// <inheritdoc />
		public string VersionString { get; }

		/// <summary>
		/// The <see cref="IConfiguration"/> for the <see cref="Application"/>
		/// </summary>
		readonly Microsoft.Extensions.Configuration.IConfiguration configuration;

		/// <summary>
		/// The <see cref="Microsoft.AspNetCore.Hosting.IHostingEnvironment"/> for the <see cref="Application"/>
		/// </summary>
		readonly Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment;

		readonly TaskCompletionSource<object> startupTcs;

		/// <summary>
		/// The <see cref="IServerAddressesFeature"/> for the <see cref="Application"/>
		/// </summary>
		IServerAddressesFeature serverAddresses;

		/// <summary>
		/// Construct an <see cref="Application"/>
		/// </summary>
		/// <param name="configuration">The value of <see cref="configuration"/></param>
		/// <param name="hostingEnvironment">The value of <see cref="hostingEnvironment"/></param>
		public Application(Microsoft.Extensions.Configuration.IConfiguration configuration, Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment)
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
#pragma warning disable CA1822 // Mark members as static
		public void ConfigureServices(IServiceCollection services)
#pragma warning restore CA1822 // Mark members as static
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));

			services.Configure<UpdatesConfiguration>(configuration.GetSection(UpdatesConfiguration.Section));
			var databaseConfigurationSection = configuration.GetSection(DatabaseConfiguration.Section);
			services.Configure<DatabaseConfiguration>(databaseConfigurationSection);
			var generalConfigurationSection = configuration.GetSection(GeneralConfiguration.Section);
			services.Configure<GeneralConfiguration>(generalConfigurationSection);

			//remember, anything you .Get manually can be null if the config is missing
			var generalConfiguration = generalConfigurationSection.Get<GeneralConfiguration>();
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			var ioManager = new DefaultIOManager();

			if (generalConfiguration?.DisableFileLogging != true)
			{
				var logPath = !String.IsNullOrEmpty(generalConfiguration?.LogFileDirectory) ? generalConfiguration.LogFileDirectory : ioManager.ConcatPath(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), VersionPrefix, "Logs");				
				services.AddLogging(builder => builder.AddFile(ioManager.ConcatPath(logPath, "tgs-{Date}.log")));
			}

			services.AddOptions();

			const string scheme = "JwtBearer";
			services.AddAuthentication((options) =>
			{
				options.DefaultAuthenticateScheme = scheme;
				options.DefaultChallengeScheme = scheme;
			}).AddJwtBearer(scheme, jwtBearerOptions =>
			{
				jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(TokenFactory.TokenSigningKey),

					ValidateIssuer = true,
					ValidIssuer = TokenFactory.TokenIssuer,

					ValidateLifetime = true,
					ValidateAudience = true,
					ValidAudience = TokenFactory.TokenAudience,

					ClockSkew = TimeSpan.FromMinutes(5),

					RequireSignedTokens = true,

					RequireExpirationTime = true
				};
				jwtBearerOptions.Events = new JwtBearerEvents
				{
					OnTokenValidated = ApiController.OnTokenValidated
				};
			});
			JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); //fucking converts 'sub' to M$ bs

			services.AddMvc().AddJsonOptions(options =>
			{
				options.AllowInputFormatterExceptionMessages = true;
				options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
				options.SerializerSettings.CheckAdditionalContent = true;
				options.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Error;
				options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
			});

			var databaseConfiguration = databaseConfigurationSection.Get<DatabaseConfiguration>();

			void ConfigureDatabase(DbContextOptionsBuilder builder)
			{
				if (hostingEnvironment.IsDevelopment())
					builder.EnableSensitiveDataLogging();
			};

			switch (databaseConfiguration?.DatabaseType ?? DatabaseType.Sqlite)
			{
				case DatabaseType.MySql:
					services.AddDbContext<MySqlDatabaseContext>(ConfigureDatabase);
					services.AddScoped<IDatabaseContext>(x => x.GetRequiredService<MySqlDatabaseContext>());
					break;
				case DatabaseType.Sqlite:
					services.AddDbContext<SqliteDatabaseContext>(ConfigureDatabase);
					services.AddScoped<IDatabaseContext>(x => x.GetRequiredService<SqliteDatabaseContext>());
					break;
				case DatabaseType.SqlServer:
					services.AddDbContext<SqlServerDatabaseContext>(ConfigureDatabase);
					services.AddScoped<IDatabaseContext>(x => x.GetRequiredService<SqlServerDatabaseContext>());
					break;
				default:
					throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid {0}!", nameof(DatabaseType)));
			}
			
			services.AddScoped<IAuthenticationContextFactory, AuthenticationContextFactory>();
			services.AddSingleton<IIdentityCache, IdentityCache>();

			services.AddSingleton<ICryptographySuite, CryptographySuite>();
			services.AddSingleton<IDatabaseSeeder, DatabaseSeeder>();
			services.AddSingleton<IPasswordHasher<Models.User>, PasswordHasher<Models.User>>();
			services.AddSingleton<ITokenFactory, TokenFactory>();
			services.AddSingleton<ISynchronousIOManager, SynchronousIOManager>();

			services.AddSingleton<IGitHubClientFactory, GitHubClientFactory>();
			services.AddSingleton(x => x.GetRequiredService<IGitHubClientFactory>().CreateClient());

			if (isWindows)
			{
				services.AddSingleton<ISystemIdentityFactory, WindowsSystemIdentityFactory>();
				services.AddSingleton<ISymlinkFactory, WindowsSymlinkFactory>();
				services.AddSingleton<IByondInstaller, WindowsByondInstaller>();
			}
			else
			{
				services.AddSingleton<ISystemIdentityFactory, PosixSystemIdentityFactory>();
				services.AddSingleton<ISymlinkFactory, PosixSymlinkFactory>();
				services.AddSingleton<IByondInstaller, PosixByondInstaller>();
			}

			services.AddSingleton<IExecutor, Executor>();
			services.AddSingleton<IScriptExecutor, ScriptExecutor>();
			services.AddSingleton<IProviderFactory, ProviderFactory>();
			services.AddSingleton<IByondTopicSender>(new ByondTopicSender
			{
				ReceiveTimeout = 5000,
				SendTimeout = 5000
			});

			services.AddSingleton<InstanceFactory>();
			services.AddSingleton<IInstanceFactory>(x => x.GetRequiredService<InstanceFactory>());
			services.AddSingleton<InstanceManager>();
			services.AddSingleton<IInstanceManager>(x => x.GetRequiredService<InstanceManager>());
			services.AddSingleton<IHostedService>(x => x.GetRequiredService<InstanceManager>());
			
			services.AddSingleton<IJobManager, JobManager>();

			services.AddSingleton<IIOManager>(ioManager);

			services.AddSingleton<DatabaseContextFactory>();
			services.AddSingleton<IDatabaseContextFactory>(x => x.GetRequiredService<DatabaseContextFactory>());

			services.AddSingleton<IApplication>(this);
		}

		/// <summary>
		/// Configure the <see cref="Application"/>
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="Application"/></param>
		public void Configure(IApplicationBuilder applicationBuilder, ILogger<Application> logger)
		{
			if (applicationBuilder == null)
				throw new ArgumentNullException(nameof(applicationBuilder));
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));

			logger.LogInformation(VersionString);

			serverAddresses = applicationBuilder.ServerFeatures.Get<IServerAddressesFeature>();
			
			applicationBuilder.UseDeveloperExceptionPage();	//it is not worth it to limit this, you should only ever get it if you're an authorized user

			applicationBuilder.UseAsyncInitialization(async cancellationToken =>
			{
				using (cancellationToken.Register(() => startupTcs.SetCanceled()))
					await startupTcs.Task.ConfigureAwait(false);
			});

			applicationBuilder.UseAuthentication();
			applicationBuilder.UseMvc();
		}

		///<inheritdoc />
		public void Ready(Exception initializationError)
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