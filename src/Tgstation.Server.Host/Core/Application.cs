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
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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

		bool CheckRunSetupWizard(IIOManager ioManager)
		{
			if (!Environment.UserInteractive)
				return false;
			var userConfigFileName = String.Format(CultureInfo.InvariantCulture, "appsettings.{0}.json", hostingEnvironment.EnvironmentName);
			var existenceTask = ioManager.FileExists(userConfigFileName, default);
			var exists = existenceTask.GetAwaiter().GetResult();

			if (exists)
			{
				var readTask = ioManager.ReadAllBytes(userConfigFileName, default);
				var bytes = readTask.GetAwaiter().GetResult();
				var contents = Encoding.UTF8.GetString(bytes);
				if (!String.IsNullOrWhiteSpace(contents))
					return false;
			}

			//non-present or empty config json
			//make our own with blackjack and hookers

			Console.WriteLine("Welcome to tgstation-server 4!");
			Console.WriteLine("This wizard will help you configure your server.");
			Console.WriteLine();
			Console.WriteLine("What port would you like to connect to TGS on?");
			Console.WriteLine("Note: If this is a docker container with the default port already mapped, use the default.");

			ushort? port = null;
			do
			{
				Console.Write("Port (leave blank for default of 5000): ");
				var portString = Console.ReadLine();
				if (String.IsNullOrWhiteSpace(portString))
					break;
				if (UInt16.TryParse(portString, out var concretePort) && concretePort != 0)
				{
					port = concretePort;
					break;
				}
				Console.WriteLine("Invalid port! Please enter a value between 1 and 65535");
			}
			while (true);


			string GetPassword()
			{
				var pwd = new StringBuilder();
				do
				{
					var i = Console.ReadKey(true);
					if (i.Key == ConsoleKey.Enter)
						break;
					else if (i.Key == ConsoleKey.Backspace)
					{
						if (pwd.Length > 0)
						{
							--pwd.Length;
							Console.Write("\b \b");
						}
					}
					else if (i.KeyChar != '\u0000') // KeyChar == '\u0000' if the key pressed does not correspond to a printable character, e.g. F1, Pause-Break, etc
					{
						pwd.Append(i.KeyChar);
						Console.Write("*");
					}
				}
				while (true);
				Console.WriteLine();
				return pwd.ToString();
			}

			DatabaseConfiguration databaseConfiguration;
			do
			{
				Console.WriteLine();
				Console.WriteLine("What SQL database type will you be using?");

				databaseConfiguration = new DatabaseConfiguration();
				do
				{
					Console.Write(String.Format(CultureInfo.InvariantCulture, "Please enter one of {0}, {1}, or {2}: ", DatabaseType.MariaDB, DatabaseType.SqlServer, DatabaseType.MySql));
					var databaseTypeString = Console.ReadLine();
					if (Enum.TryParse<DatabaseType>(databaseTypeString, out var databaseType))
					{
						databaseConfiguration.DatabaseType = databaseType;
						break;
					}
					Console.WriteLine("Invalid database type!");
				}
				while (true);

				Console.WriteLine();
				Console.Write("Enter the server's address and port (blank for local): ");
				var serverAddress = Console.ReadLine();
				if (String.IsNullOrWhiteSpace(serverAddress))
					serverAddress = null;

				Console.WriteLine();
				Console.Write("Enter the database name (Can be from previous installation. Otherwise, should not exist): ");
				var databaseName = Console.ReadLine();

				bool dbExists;
				do
				{
					Console.Write("Does this database already exist? (y/n): ");
					var responseString = Console.ReadLine();
					var upperResponse = responseString.ToUpperInvariant();
					if (upperResponse == "Y" || upperResponse == "YES")
					{
						dbExists = true;
						break;
					}
					else if (upperResponse == "N" || upperResponse == "NO")
					{
						dbExists = false;
						break;
					}
					Console.WriteLine("Invalid response!");
				}
				while (true);

				bool? useWinAuth;
				if (databaseConfiguration.DatabaseType == DatabaseType.SqlServer && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					do
					{
						Console.Write("Use Windows Authentication? (y/n): ");
						var responseString = Console.ReadLine();
						var upperResponse = responseString.ToUpperInvariant();
						if (upperResponse == "Y" || upperResponse == "YES")
						{
							useWinAuth = true;
							break;
						}
						else if (upperResponse == "N" || upperResponse == "NO")
						{
							useWinAuth = false;
							break;
						}
						Console.WriteLine("Invalid response!");
					}
					while (true);
				else
					useWinAuth = null;

				Console.WriteLine();

				string username = null;
				string password = null;
				if (useWinAuth != true)
				{
					Console.Write("Enter username: ");
					username = Console.ReadLine();
					Console.Write("Enter password: ");
					password = GetPassword();
					Console.WriteLine();
				}

				IDbConnection testConnection;
				if (databaseConfiguration.DatabaseType == DatabaseType.SqlServer)
				{
					var csb = new SqlConnectionStringBuilder
					{
						ApplicationName = VersionPrefix,
						DataSource = serverAddress ?? "(local)"
					};
					if (useWinAuth.Value)
						csb.IntegratedSecurity = true;
					else
					{
						csb.UserID = username;
						csb.Password = password;
					}
					testConnection = new SqlConnection
					{
						ConnectionString = csb.ConnectionString
					};
					csb.InitialCatalog = databaseName;
					databaseConfiguration.ConnectionString = csb.ConnectionString;
				}
				else
				{
					var csb = new MySqlConnectionStringBuilder
					{
						Server = serverAddress ?? "127.0.0.1",
						UserID = username,
						Password = password
					};
					testConnection = new MySqlConnection
					{
						ConnectionString = csb.ConnectionString
					};
					csb.Database = databaseName;
					databaseConfiguration.ConnectionString = csb.ConnectionString;
				}

				try
				{
					using (testConnection)
					{
						Console.WriteLine("Testing connection...");
						testConnection.Open();
						Console.WriteLine("Connection successful!");

						if (databaseConfiguration.DatabaseType != DatabaseType.SqlServer)
						{
							Console.WriteLine("Checking MySQL/MariaDB version...");
							using (var command = testConnection.CreateCommand())
							{
								command.CommandText = "SELECT VERSION()";
								var fullVersion = (string)command.ExecuteScalar();
								Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "Found {0}", fullVersion));
								var splits = fullVersion.Split('-');
								databaseConfiguration.MySqlServerVersion = splits[0];
							}
						}

						if (!dbExists)
						{
							Console.WriteLine("Testing create DB permission...");
							using (var command = testConnection.CreateCommand())
							{
								command.CommandText = String.Format(CultureInfo.InvariantCulture, "CREATE DATABASE {0}", databaseName);
								command.ExecuteNonQuery();
							}
							Console.WriteLine("Success!");
							Console.WriteLine("Dropping test database...");
							using (var command = testConnection.CreateCommand())
							{
								command.CommandText = String.Format(CultureInfo.InvariantCulture, "DROP DATABASE {0}", databaseName);
								try
								{
									command.ExecuteNonQuery();
								}
								catch (Exception e)
								{
									Console.WriteLine(e.Message);
									Console.WriteLine();
									Console.WriteLine("This should be okay, but you may want to manually drop the database before continuing!");
									Console.WriteLine("Press any key to continue...");
									Console.ReadKey();
								}
							}
						}
					}

					break;
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					Console.WriteLine();
					Console.WriteLine("Retrying database configuration...");
				}
			} while (true);

			var generalConfiguration = new GeneralConfiguration
			{
				MinimumPasswordLength = 15
			};
			do
			{
				Console.WriteLine();
				Console.Write("Minimum database user password length (leave blank for default of 15): ");
				var passwordLengthString = Console.ReadLine();
				if (String.IsNullOrWhiteSpace(passwordLengthString))
					break;
				if (UInt32.TryParse(passwordLengthString, out var passwordLength))
				{
					generalConfiguration.MinimumPasswordLength = passwordLength;
					break;
				}
				Console.WriteLine("Please enter a positive integer!");
			}
			while (true);

			Console.WriteLine();
			Console.WriteLine("Enter a GitHub personal access token to bypass some rate limits (this is optional and does not require any scopes)");
			Console.Write("GitHub personal access token: ");
			generalConfiguration.GitHubAccessToken = GetPassword();
			if (String.IsNullOrWhiteSpace(generalConfiguration.GitHubAccessToken))
				generalConfiguration.GitHubAccessToken = null;

			Console.WriteLine();
			Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "Configuration complete! Saving to {0}", userConfigFileName));

			var map = new Dictionary<string, object>()
			{
				{ DatabaseConfiguration.Section, databaseConfiguration },
				{ GeneralConfiguration.Section, generalConfiguration }
			};

			if (port.HasValue)
				map.Add("Kestrel", new {
					EndPoints = new
					{
						Http = new
						{
							Url = String.Format(CultureInfo.InvariantCulture, "http://0.0.0.0:{0}", port)
						}
					}
				});

			var json = JsonConvert.SerializeObject(map, Formatting.Indented);
			var configBytes = Encoding.UTF8.GetBytes(json);

			var writeTask = ioManager.WriteAllBytes(userConfigFileName, configBytes, default);
			try
			{
				writeTask.GetAwaiter().GetResult();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				Console.WriteLine();
				Console.WriteLine("For your convienence, here's the text we tried to write out:");
				Console.WriteLine();
				Console.WriteLine(json);
				Console.WriteLine();
				Console.WriteLine("Press any key to exit...");
				Console.ReadKey();
				throw new OperationCanceledException();
			}

			Console.WriteLine("Waiting for configuration changes to reload...");
			Task.Delay(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();

			return true;
		}

		/// <summary>
		/// Configure dependency injected services
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to configure</param>
		public void ConfigureServices(IServiceCollection services)
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));

			services.Configure<UpdatesConfiguration>(configuration.GetSection(UpdatesConfiguration.Section));
			services.Configure<DatabaseConfiguration>(configuration.GetSection(DatabaseConfiguration.Section));
			services.Configure<GeneralConfiguration>(configuration.GetSection(GeneralConfiguration.Section));
			services.Configure<FileLoggingConfiguration>(configuration.GetSection(FileLoggingConfiguration.Section));

			services.AddOptions();

			var ioManager = new DefaultIOManager();
			var ranWizard = CheckRunSetupWizard(ioManager);

			GeneralConfiguration generalConfiguration;
			DatabaseConfiguration databaseConfiguration;
			FileLoggingConfiguration fileLoggingConfiguration;
			using (var provider = services.BuildServiceProvider())
			{
				var generalOptions = provider.GetRequiredService<IOptions<GeneralConfiguration>>();
				generalConfiguration = generalOptions.Value;

				if (generalConfiguration.ConfigCheckOnly)
					throw new OperationCanceledException("Configuration check complete!");

				if (ranWizard)
					Console.WriteLine("Now launching TGS...");

				var dbOptions = provider.GetRequiredService<IOptions<DatabaseConfiguration>>();
				databaseConfiguration = dbOptions.Value;

				var loggingOptions = provider.GetRequiredService<IOptions<FileLoggingConfiguration>>();
				fileLoggingConfiguration = loggingOptions.Value;
			}


			if (!fileLoggingConfiguration.Disable)
			{
				var logPath = !String.IsNullOrEmpty(fileLoggingConfiguration.Directory) ? fileLoggingConfiguration.Directory : ioManager.ConcatPath(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), VersionPrefix, "Logs");

				logPath = ioManager.ConcatPath(logPath, "tgs-{Date}.log");

				services.AddLogging(builder =>
				{
					LogLevel GetMinimumLogLevel(string stringLevel)
					{
						if (String.IsNullOrWhiteSpace(stringLevel) || !Enum.TryParse<LogLevel>(stringLevel, out var minimumLevel))
							minimumLevel = LogLevel.Information;
						return minimumLevel;
					}

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

					var logEventLevel = ConvertLogLevel(GetMinimumLogLevel(fileLoggingConfiguration.LogLevel));
					var microsoftEventLevel = ConvertLogLevel(GetMinimumLogLevel(fileLoggingConfiguration.MicrosoftLogLevel));

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
			}

			services.AddScoped<IClaimsInjector, ClaimsInjector>();

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

					ClockSkew = TimeSpan.FromMinutes(1),

					RequireSignedTokens = true,

					RequireExpirationTime = true
				};
				jwtBearerOptions.Events = new JwtBearerEvents
				{
					//Application is our composition root so this monstrosity of a line is okay
					OnTokenValidated = ctx => ctx.HttpContext.RequestServices.GetRequiredService<IClaimsInjector>().InjectClaimsIntoContext(ctx, ctx.HttpContext.RequestAborted)
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

			services.AddScoped<IAuthenticationContextFactory, AuthenticationContextFactory>();
			services.AddSingleton<IIdentityCache, IdentityCache>();

			services.AddSingleton<ICryptographySuite, CryptographySuite>();
			services.AddSingleton<IDatabaseSeeder, DatabaseSeeder>();
			services.AddSingleton<IPasswordHasher<Models.User>, PasswordHasher<Models.User>>();
			services.AddSingleton<ITokenFactory, TokenFactory>();
			services.AddSingleton<ISynchronousIOManager, SynchronousIOManager>();
			services.AddSingleton<ICredentialsProvider, CredentialsProvider>();

			services.AddSingleton<IGitHubClientFactory, GitHubClientFactory>();

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

			services.AddSingleton<IProcessExecutor, ProcessExecutor>();
			services.AddSingleton<IProviderFactory, ProviderFactory>();
			services.AddSingleton<IByondTopicSender>(new ByondTopicSender
			{
				ReceiveTimeout = 5000,
				SendTimeout = 5000
			});

			services.AddSingleton<IChatFactory, ChatFactory>();
			services.AddSingleton<IWatchdogFactory, WatchdogFactory>();
			services.AddSingleton<IInstanceFactory, InstanceFactory>();

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
			ChangeToken.OnChange(configuration.GetReloadToken, () => serverControl.Restart());

			applicationBuilder.UseDeveloperExceptionPage(); //it is not worth it to limit this, you should only ever get it if you're an authorized user
			
			applicationBuilder.UseCancelledRequestSuppression();

			applicationBuilder.UseAsyncInitialization(async cancellationToken =>
			{
				using (cancellationToken.Register(() => startupTcs.SetCanceled()))
					await startupTcs.Task.ConfigureAwait(false);
			});

			applicationBuilder.UseAuthentication();

			applicationBuilder.UseDbConflictHandling();

			applicationBuilder.UseMvc();
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