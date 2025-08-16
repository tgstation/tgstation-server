using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MySqlConnector;

using Npgsql;

using Tgstation.Server.Common;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Properties;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;
using Tgstation.Server.Shared;

using YamlDotNet.Serialization;

namespace Tgstation.Server.Host.Setup
{
	/// <inheritdoc />
	sealed class SetupWizard : BackgroundService
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IConsole"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly IConsole console;

		/// <summary>
		/// The <see cref="IHostEnvironment"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly IHostEnvironment hostingEnvironment;

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="IDatabaseConnectionFactory"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly IDatabaseConnectionFactory dbConnectionFactory;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="IHostApplicationLifetime"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly IHostApplicationLifetime applicationLifetime;

		/// <summary>
		/// The <see cref="IPostSetupServices"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly IPostSetupServices postSetupServices;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// The <see cref="InternalConfiguration"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly InternalConfiguration internalConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="SetupWizard"/> class.
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="console">The value of <see cref="console"/>.</param>
		/// <param name="hostingEnvironment">The value of <see cref="hostingEnvironment"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="dbConnectionFactory">The value of <see cref="dbConnectionFactory"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="applicationLifetime">The value of <see cref="applicationLifetime"/>.</param>
		/// <param name="postSetupServices">The value of <see cref="postSetupServices"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="internalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="internalConfiguration"/>.</param>
		public SetupWizard(
			IIOManager ioManager,
			IConsole console,
			IHostEnvironment hostingEnvironment,
			IAssemblyInformationProvider assemblyInformationProvider,
			IDatabaseConnectionFactory dbConnectionFactory,
			IPlatformIdentifier platformIdentifier,
			IAsyncDelayer asyncDelayer,
			IHostApplicationLifetime applicationLifetime,
			IPostSetupServices postSetupServices,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<InternalConfiguration> internalConfigurationOptions)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.console = console ?? throw new ArgumentNullException(nameof(console));
			this.hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
			this.postSetupServices = postSetupServices ?? throw new ArgumentNullException(nameof(postSetupServices));

			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			internalConfiguration = internalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(internalConfigurationOptions));
		}

		/// <inheritdoc />
		protected override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			await CheckRunWizard(cancellationToken);
			applicationLifetime.StopApplication();
		}

		/// <summary>
		/// A prompt for a yes or no value.
		/// </summary>
		/// <param name="question">The question <see cref="string"/>.</param>
		/// <param name="defaultResponse">The optional default response if the user doesn't enter anything.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> resulting in <see langword="true"/> if the user replied yes, <see langword="false"/> otherwise.</returns>
		async ValueTask<bool> PromptYesNo(string question, bool? defaultResponse, CancellationToken cancellationToken)
		{
			do
			{
				await console.WriteAsync($"{question} ({(defaultResponse == true ? 'Y' : 'y')}/{(defaultResponse == false ? 'N' : 'n')}): ", false, cancellationToken);
				var responseString = await console.ReadLineAsync(false, cancellationToken);
				if (responseString.Length == 0)
				{
					if (defaultResponse.HasValue)
						return defaultResponse.Value;
				}
				else
				{
					var upperResponse = responseString.ToUpperInvariant();
					if (upperResponse == "Y" || upperResponse == "YES")
						return true;
					else if (upperResponse == "N" || upperResponse == "NO")
						return false;
				}

				await console.WriteAsync("Invalid response!", true, cancellationToken);
			}
			while (true);
		}

		/// <summary>
		/// Prompts the user to enter the port to host TGS on.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> resulting in the hosting port, or <see langword="null"/> to use the default.</returns>
		async ValueTask<HostingSpecification?> PromptForHostingSpec(CancellationToken cancellationToken)
		{
			await console.WriteAsync(null, true, cancellationToken);
			await console.WriteAsync("What port would you like to connect to TGS on? (Bridge port will alsoe be set to this)", true, cancellationToken);
			await console.WriteAsync("Note: If this is a docker container with the default port already mapped, use the default.", true, cancellationToken);

			do
			{
				await console.WriteAsync(
					$"API Port (leave blank for default of {GeneralConfiguration.DefaultApiPort}): ",
					false,
					cancellationToken);
				var portString = await console.ReadLineAsync(false, cancellationToken);
				if (String.IsNullOrWhiteSpace(portString))
					return null;
				if (UInt16.TryParse(portString, out var port) && port != 0)
					return new HostingSpecification
					{
						Port = port,
					};
				await console.WriteAsync("Invalid port! Please enter a value between 1 and 65535", true, cancellationToken);
			}
			while (true);
		}

		/// <summary>
		/// Ensure a given <paramref name="testConnection"/> works.
		/// </summary>
		/// <param name="testConnection">The test <see cref="DbConnection"/>.</param>
		/// <param name="databaseConfiguration">The <see cref="DatabaseConfiguration"/> may have derived data populated.</param>
		/// <param name="databaseName">The database name (or path in the case of a <see cref="DatabaseType.Sqlite"/> database).</param>
		/// <param name="dbExists">Whether or not the database exists.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask TestDatabaseConnection(
			DbConnection testConnection,
			DatabaseConfiguration databaseConfiguration,
			string databaseName,
			bool dbExists,
			CancellationToken cancellationToken)
		{
			bool isSqliteDB = databaseConfiguration.DatabaseType == DatabaseType.Sqlite;
			using (testConnection)
			{
				await console.WriteAsync("Testing connection...", true, cancellationToken);
				await testConnection.OpenAsync(cancellationToken);
				await console.WriteAsync("Connection successful!", true, cancellationToken);

				if (databaseConfiguration.DatabaseType == DatabaseType.MariaDB
					|| databaseConfiguration.DatabaseType == DatabaseType.MySql
					|| databaseConfiguration.DatabaseType == DatabaseType.PostgresSql)
				{
					await console.WriteAsync($"Checking {databaseConfiguration.DatabaseType} version...", true, cancellationToken);
					using var command = testConnection.CreateCommand();
					command.CommandText = "SELECT VERSION()";
					var fullVersion = (string?)await command.ExecuteScalarAsync(cancellationToken);
					await console.WriteAsync(String.Format(CultureInfo.InvariantCulture, "Found {0}", fullVersion), true, cancellationToken);

					if (fullVersion == null)
						throw new InvalidOperationException($"\"{command.CommandText}\" returned null!");

					if (databaseConfiguration.DatabaseType == DatabaseType.PostgresSql)
					{
						var splits = fullVersion.Split(' ');
						databaseConfiguration.ServerVersion = splits[1].TrimEnd(',');
					}
					else
					{
						var splits = fullVersion.Split('-');
						databaseConfiguration.ServerVersion = splits.First();
					}
				}

				if (!isSqliteDB && !dbExists)
				{
					await console.WriteAsync("Testing create DB permission...", true, cancellationToken);
					using (var command = testConnection.CreateCommand())
					{
						// I really don't care about user sanitization here, they want to fuck their own DB? so be it
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
						command.CommandText = $"CREATE DATABASE {databaseName}";
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
						await command.ExecuteNonQueryAsync(cancellationToken);
					}

					await console.WriteAsync("Success!", true, cancellationToken);
					await console.WriteAsync("Dropping test database...", true, cancellationToken);
					using (var command = testConnection.CreateCommand())
					{
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
						command.CommandText = $"DROP DATABASE {databaseName}";
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
						try
						{
							await command.ExecuteNonQueryAsync(cancellationToken);
						}
						catch (OperationCanceledException)
						{
							throw;
						}
						catch (Exception e)
						{
							await console.WriteAsync(e.Message, true, cancellationToken);
							await console.WriteAsync(null, true, cancellationToken);
							await console.WriteAsync("This should be okay, but you may want to manually drop the database before continuing!", true, cancellationToken);
							await console.WriteAsync("Press any key to continue...", true, cancellationToken);
							await console.PressAnyKeyAsync(cancellationToken);
						}
					}
				}

				await testConnection.CloseAsync();
			}

			if (isSqliteDB && !dbExists)
			{
				await console.WriteAsync("Deleting test database file...", true, cancellationToken);
				if (platformIdentifier.IsWindows)
					SqliteConnection.ClearAllPools();
				await ioManager.DeleteFile(databaseName, cancellationToken);
			}
		}

		/// <summary>
		/// Check that a given SQLite <paramref name="databaseName"/> is can be accessed. Also prompts the user if they want to use a relative or absolute path.
		/// </summary>
		/// <param name="databaseName">The path to the potential SQLite database file.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the SQLite database path to store in the configuration.</returns>
		async ValueTask<string?> ValidateNonExistantSqliteDBName(string databaseName, CancellationToken cancellationToken)
		{
			var dbPathIsRooted = ioManager.IsPathRooted(databaseName);
			var resolvedPath = ioManager.ResolvePath(
				dbPathIsRooted
					? databaseName
					: ioManager.ConcatPath(
						internalConfiguration.AppSettingsBasePath,
						databaseName));
			try
			{
				var directoryName = ioManager.GetDirectoryName(resolvedPath);
				bool directoryExisted = await ioManager.DirectoryExists(directoryName, cancellationToken);
				await ioManager.CreateDirectory(directoryName, cancellationToken);
				try
				{
					await ioManager.WriteAllBytes(resolvedPath, Array.Empty<byte>(), cancellationToken);
				}
				catch
				{
					if (!directoryExisted)
						await ioManager.DeleteDirectory(directoryName, cancellationToken);
					throw;
				}
			}
			catch (IOException)
			{
				return null;
			}

			if (!dbPathIsRooted)
			{
				await console.WriteAsync("Note, this relative path currently resolves to the following:", true, cancellationToken);
				await console.WriteAsync(resolvedPath, true, cancellationToken);
				bool writeResolved = await PromptYesNo(
					"Would you like to save the relative path in the configuration? If not, the full path will be saved.",
					null,
					cancellationToken);

				if (writeResolved)
					databaseName = resolvedPath;
			}

			await ioManager.DeleteFile(databaseName, cancellationToken);
			return databaseName;
		}

		/// <summary>
		/// Prompt the user for the <see cref="DatabaseType"/>.
		/// </summary>
		/// <param name="firstTime">If this is the user's first time here.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the input <see cref="DatabaseType"/>.</returns>
		async ValueTask<DatabaseType> PromptDatabaseType(bool firstTime, CancellationToken cancellationToken)
		{
			if (firstTime)
			{
				if (internalConfiguration.MariaDBSetup)
				{
					await console.WriteAsync("It looks like you just installed MariaDB. Selecting it as the database type.", true, cancellationToken);
					return DatabaseType.MariaDB;
				}

				await console.WriteAsync(String.Empty, true, cancellationToken);
				await console.WriteAsync(
					"NOTE: If you are serious about hosting public servers, it is HIGHLY reccommended that TGS runs on a database *OTHER THAN* Sqlite.",
					true,
					cancellationToken);
				await console.WriteAsync(
					"It is, however, the easiest option to get started with and will pose few if any problems in a single user scenario.",
					true,
					cancellationToken);
			}

			await console.WriteAsync("What SQL database type will you be using?", true, cancellationToken);
			do
			{
				await console.WriteAsync(
					String.Format(
						CultureInfo.InvariantCulture,
						"Please enter one of {0}, {1}, {2}, {3} or {4}: ",
						DatabaseType.MariaDB,
						DatabaseType.MySql,
						DatabaseType.PostgresSql,
						DatabaseType.SqlServer,
						DatabaseType.Sqlite),
					false,
					cancellationToken);
				var databaseTypeString = await console.ReadLineAsync(false, cancellationToken);
				if (Enum.TryParse<DatabaseType>(databaseTypeString, out var databaseType))
					return databaseType;

				await console.WriteAsync("Invalid database type!", true, cancellationToken);
			}
			while (true);
		}

		/// <summary>
		/// Prompts the user to create a <see cref="DatabaseConfiguration"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the new <see cref="DatabaseConfiguration"/>.</returns>
#pragma warning disable CA1502 // TODO: Decomplexify
		async ValueTask<DatabaseConfiguration> ConfigureDatabase(CancellationToken cancellationToken)
		{
			bool firstTime = true;
			do
			{
				await console.WriteAsync(null, true, cancellationToken);

				var databaseConfiguration = new DatabaseConfiguration
				{
					DatabaseType = await PromptDatabaseType(firstTime, cancellationToken),
				};

				string? serverAddress = null;
				ushort? serverPort = null;

				var definitelyLocalMariaDB = firstTime && internalConfiguration.MariaDBSetup;
				var isSqliteDB = databaseConfiguration.DatabaseType == DatabaseType.Sqlite;
				IPHostEntry? serverAddressEntry = null;
				if (!isSqliteDB)
					do
					{
						await console.WriteAsync(null, true, cancellationToken);
						if (definitelyLocalMariaDB)
						{
							await console.WriteAsync("Enter the server's port (blank for 3306): ", false, cancellationToken);
							var enteredPort = await console.ReadLineAsync(false, cancellationToken);
							if (!String.IsNullOrWhiteSpace(enteredPort) && enteredPort.Trim() != "3306")
								serverAddress = $"localhost:{enteredPort}";
						}
						else
						{
							await console.WriteAsync("Enter the server's address and port [<server>:<port> or <server>] (blank for local): ", false, cancellationToken);
							serverAddress = await console.ReadLineAsync(false, cancellationToken);
						}

						if (String.IsNullOrWhiteSpace(serverAddress))
							serverAddress = null;
						else if (databaseConfiguration.DatabaseType == DatabaseType.SqlServer)
						{
							var match = Regex.Match(serverAddress, @"^(?<server>.+):(?<port>.+)$");
							if (match.Success)
							{
								serverAddress = match.Groups["server"].Value;
								var portString = match.Groups["port"].Value;
								if (UInt16.TryParse(portString, out var port))
									serverPort = port;
								else
								{
									await console.WriteAsync($"Failed to parse port \"{portString}\", please try again.", true, cancellationToken);
									continue;
								}
							}
						}

						try
						{
							if (serverAddress != null)
							{
								await console.WriteAsync("Attempting to resolve address...", true, cancellationToken);
								serverAddressEntry = await Dns.GetHostEntryAsync(serverAddress, cancellationToken);
							}

							break;
						}
						catch (Exception ex)
						{
							await console.WriteAsync($"Unable to resolve address: {ex.Message}", true, cancellationToken);
						}
					}
					while (true);

				await console.WriteAsync(null, true, cancellationToken);
				await console.WriteAsync($"Enter the database {(isSqliteDB ? "file path" : "name")} ({(definitelyLocalMariaDB ? "leave blank for \"tgs\")" : "Can be from previous installation. Otherwise, should not exist")}): ", false, cancellationToken);

				string? databaseName;
				bool dbExists = false;
				do
				{
					databaseName = await console.ReadLineAsync(false, cancellationToken);
					if (!String.IsNullOrWhiteSpace(databaseName))
					{
						if (isSqliteDB)
						{
							dbExists = await ioManager.FileExists(databaseName, cancellationToken);
							if (!dbExists)
								databaseName = await ValidateNonExistantSqliteDBName(databaseName, cancellationToken);
						}
						else
							dbExists = await PromptYesNo(
								"Does this database already exist? If not, we will attempt to CREATE it.",
								null,
								cancellationToken);
					}
					else if (definitelyLocalMariaDB)
						databaseName = "tgs";

					if (String.IsNullOrWhiteSpace(databaseName))
						await console.WriteAsync("Invalid database name!", true, cancellationToken);
					else
						break;
				}
				while (true);

				var useWinAuth = false;
				var encrypt = false;
				if (databaseConfiguration.DatabaseType == DatabaseType.SqlServer && platformIdentifier.IsWindows)
				{
					var defaultResponse = serverAddressEntry?.AddressList.Any(IPAddress.IsLoopback) ?? false
						? (bool?)true
						: null;
					useWinAuth = await PromptYesNo("Use Windows Authentication?", defaultResponse, cancellationToken);
					encrypt = await PromptYesNo("Use encrypted connection?", false, cancellationToken);
				}

				await console.WriteAsync(null, true, cancellationToken);

				string? username = null;
				string? password = null;
				if (!isSqliteDB)
					if (!useWinAuth)
					{
						if (definitelyLocalMariaDB)
						{
							await console.WriteAsync("Using username: root", true, cancellationToken);
							username = "root";
						}
						else
						{
							await console.WriteAsync("Enter username: ", false, cancellationToken);
							username = await console.ReadLineAsync(false, cancellationToken);
						}

						await console.WriteAsync("Enter password: ", false, cancellationToken);
						password = await console.ReadLineAsync(true, cancellationToken);
					}
					else
					{
						await console.WriteAsync("IMPORTANT: If using the service runner, ensure this computer's LocalSystem account has CREATE DATABASE permissions on the target server!", true, cancellationToken);
						await console.WriteAsync("The account it uses in MSSQL is usually \"NT AUTHORITY\\SYSTEM\" and the role it needs is usually \"dbcreator\".", true, cancellationToken);
						await console.WriteAsync("We'll run a sanity test here, but it won't be indicative of the service's permissions if that is the case", true, cancellationToken);
					}

				await console.WriteAsync(null, true, cancellationToken);

				DbConnection testConnection;
				void CreateTestConnection(string connectionString) =>
					testConnection = dbConnectionFactory.CreateConnection(
						connectionString,
						databaseConfiguration.DatabaseType);

				switch (databaseConfiguration.DatabaseType)
				{
					case DatabaseType.SqlServer:
						{
							var csb = new SqlConnectionStringBuilder
							{
								ApplicationName = assemblyInformationProvider.VersionPrefix,
								DataSource = serverAddress ?? "(local)",
								Encrypt = encrypt,
							};

							if (useWinAuth)
								csb.IntegratedSecurity = true;
							else
							{
								csb.UserID = username;
								csb.Password = password;
							}

							csb.Encrypt = encrypt;

							CreateTestConnection(csb.ConnectionString);
							csb.InitialCatalog = databaseName;
							databaseConfiguration.ConnectionString = csb.ConnectionString;
						}

						break;
					case DatabaseType.MariaDB:
					case DatabaseType.MySql:
						{
							// MySQL/MariaDB
							var csb = new MySqlConnectionStringBuilder
							{
								Server = serverAddress ?? "127.0.0.1",
								UserID = username,
								Password = password,
							};

							if (serverPort.HasValue)
								csb.Port = serverPort.Value;

							CreateTestConnection(csb.ConnectionString);
							csb.Database = databaseName;
							databaseConfiguration.ConnectionString = csb.ConnectionString;
						}

						break;
					case DatabaseType.Sqlite:
						{
							var csb = new SqliteConnectionStringBuilder
							{
								DataSource = databaseName,
								Mode = dbExists ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate,
							};

							CreateTestConnection(csb.ConnectionString);

							csb.Mode = SqliteOpenMode.ReadWriteCreate;
							databaseConfiguration.ConnectionString = csb.ConnectionString;
						}

						break;
					case DatabaseType.PostgresSql:
						{
							var csb = new NpgsqlConnectionStringBuilder
							{
								ApplicationName = assemblyInformationProvider.VersionPrefix,
								Host = serverAddress ?? "127.0.0.1",
								Password = password,
								Username = username,
							};

							if (serverPort.HasValue)
								csb.Port = serverPort.Value;

							CreateTestConnection(csb.ConnectionString);
							csb.Database = databaseName;
							databaseConfiguration.ConnectionString = csb.ConnectionString;
						}

						break;
					default:
						throw new InvalidOperationException("Invalid DatabaseType!");
				}

				try
				{
					await TestDatabaseConnection(testConnection, databaseConfiguration, databaseName, dbExists, cancellationToken);

					return databaseConfiguration;
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception e)
				{
					await console.WriteAsync(e.Message, true, cancellationToken);
					await console.WriteAsync(null, true, cancellationToken);
					await console.WriteAsync("Retrying database configuration...", true, cancellationToken);

					if (definitelyLocalMariaDB)
						await console.WriteAsync("No longer assuming MariaDB is the target.", true, cancellationToken);

					firstTime = false;
				}
			}
			while (true);
		}
#pragma warning restore CA1502

		/// <summary>
		/// Prompts the user to create a <see cref="GeneralConfiguration"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the new <see cref="GeneralConfiguration"/>.</returns>
		async ValueTask<GeneralConfiguration> ConfigureGeneral(CancellationToken cancellationToken)
		{
			var newGeneralConfiguration = new GeneralConfiguration
			{
				SetupWizardMode = SetupWizardMode.Never,
			};

			do
			{
				await console.WriteAsync(null, true, cancellationToken);
				await console.WriteAsync(String.Format(CultureInfo.InvariantCulture, "Minimum database user password length (leave blank for default of {0}): ", newGeneralConfiguration.MinimumPasswordLength), false, cancellationToken);
				var passwordLengthString = await console.ReadLineAsync(false, cancellationToken);
				if (String.IsNullOrWhiteSpace(passwordLengthString))
					break;
				if (UInt32.TryParse(passwordLengthString, out var passwordLength) && passwordLength >= 0)
				{
					newGeneralConfiguration.MinimumPasswordLength = passwordLength;
					break;
				}

				await console.WriteAsync("Please enter a positive integer!", true, cancellationToken);
			}
			while (true);

			do
			{
				await console.WriteAsync(null, true, cancellationToken);
				await console.WriteAsync(String.Format(CultureInfo.InvariantCulture, "Default timeout for sending and receiving BYOND topics (ms, 0 for infinite, leave blank for default of {0}): ", newGeneralConfiguration.ByondTopicTimeout), false, cancellationToken);
				var topicTimeoutString = await console.ReadLineAsync(false, cancellationToken);
				if (String.IsNullOrWhiteSpace(topicTimeoutString))
					break;
				if (UInt32.TryParse(topicTimeoutString, out var topicTimeout) && topicTimeout >= 0)
				{
					newGeneralConfiguration.ByondTopicTimeout = topicTimeout;
					break;
				}

				await console.WriteAsync("Please enter a positive integer!", true, cancellationToken);
			}
			while (true);

			await console.WriteAsync(null, true, cancellationToken);
			await console.WriteAsync("Enter a classic GitHub personal access token to bypass some rate limits (this is optional and does not require any scopes)", true, cancellationToken);
			await console.WriteAsync("GitHub personal access token: ", false, cancellationToken);
			newGeneralConfiguration.GitHubAccessToken = await console.ReadLineAsync(true, cancellationToken);
			if (String.IsNullOrWhiteSpace(newGeneralConfiguration.GitHubAccessToken))
				newGeneralConfiguration.GitHubAccessToken = null;

			newGeneralConfiguration.HostApiDocumentation = await PromptYesNo("Host API Documentation?", false, cancellationToken);

			return newGeneralConfiguration;
		}

		/// <summary>
		/// Prompts the user to create a <see cref="FileLoggingConfiguration"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the new <see cref="FileLoggingConfiguration"/>.</returns>
		async ValueTask<FileLoggingConfiguration> ConfigureLogging(CancellationToken cancellationToken)
		{
			var fileLoggingConfiguration = new FileLoggingConfiguration();
			await console.WriteAsync(null, true, cancellationToken);
			fileLoggingConfiguration.Disable = !await PromptYesNo("Enable file logging?", true, cancellationToken);

			if (!fileLoggingConfiguration.Disable)
			{
				do
				{
					await console.WriteAsync("Log file directory path (leave blank for default): ", false, cancellationToken);
					fileLoggingConfiguration.Directory = await console.ReadLineAsync(false, cancellationToken);
					if (String.IsNullOrWhiteSpace(fileLoggingConfiguration.Directory))
					{
						fileLoggingConfiguration.Directory = null;
						break;
					}

					// test a write of it
					await console.WriteAsync(null, true, cancellationToken);
					await console.WriteAsync("Testing directory access...", true, cancellationToken);
					try
					{
						await ioManager.CreateDirectory(fileLoggingConfiguration.Directory, cancellationToken);
						var testFile = ioManager.ConcatPath(fileLoggingConfiguration.Directory, String.Format(CultureInfo.InvariantCulture, "WizardAccesTest.{0}.deleteme", Guid.NewGuid()));
						await ioManager.WriteAllBytes(testFile, Array.Empty<byte>(), cancellationToken);
						try
						{
							await ioManager.DeleteFile(testFile, cancellationToken);
						}
						catch (OperationCanceledException)
						{
							throw;
						}
						catch (Exception e)
						{
							await console.WriteAsync(String.Format(CultureInfo.InvariantCulture, "Error deleting test log file: {0}", testFile), true, cancellationToken);
							await console.WriteAsync(e.Message, true, cancellationToken);
							await console.WriteAsync(null, true, cancellationToken);
						}

						break;
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch (Exception e)
					{
						await console.WriteAsync(e.Message, true, cancellationToken);
						await console.WriteAsync(null, true, cancellationToken);
						await console.WriteAsync("Please verify the path is valid and you have access to it!", true, cancellationToken);
					}
				}
				while (true);

				async ValueTask<LogLevel?> PromptLogLevel(string question)
				{
					do
					{
						await console.WriteAsync(null, true, cancellationToken);
						await console.WriteAsync(question, true, cancellationToken);
						await console.WriteAsync(String.Format(CultureInfo.InvariantCulture, "Enter one of {0}/{1}/{2}/{3}/{4}/{5} (leave blank for default): ", nameof(LogLevel.Trace), nameof(LogLevel.Debug), nameof(LogLevel.Information), nameof(LogLevel.Warning), nameof(LogLevel.Error), nameof(LogLevel.Critical)), false, cancellationToken);
						var responseString = await console.ReadLineAsync(false, cancellationToken);
						if (String.IsNullOrWhiteSpace(responseString))
							return null;
						if (Enum.TryParse<LogLevel>(responseString, out var logLevel) && logLevel != LogLevel.None)
							return logLevel;
						await console.WriteAsync("Invalid log level!", true, cancellationToken);
					}
					while (true);
				}

				fileLoggingConfiguration.LogLevel = await PromptLogLevel(String.Format(CultureInfo.InvariantCulture, "Enter the level limit for normal logs (default {0}).", fileLoggingConfiguration.LogLevel)) ?? fileLoggingConfiguration.LogLevel;
				fileLoggingConfiguration.MicrosoftLogLevel = await PromptLogLevel(String.Format(CultureInfo.InvariantCulture, "Enter the level limit for Microsoft logs (VERY verbose, default {0}).", fileLoggingConfiguration.MicrosoftLogLevel)) ?? fileLoggingConfiguration.MicrosoftLogLevel;
			}

			return fileLoggingConfiguration;
		}

		/// <summary>
		/// Prompts the user to create a <see cref="ElasticsearchConfiguration"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the new <see cref="ElasticsearchConfiguration"/>.</returns>
		async ValueTask<ElasticsearchConfiguration> ConfigureElasticsearch(CancellationToken cancellationToken)
		{
			var elasticsearchConfiguration = new ElasticsearchConfiguration();
			await console.WriteAsync(null, true, cancellationToken);
			elasticsearchConfiguration.Enable = await PromptYesNo("Enable logging to an external ElasticSearch server?", false, cancellationToken);

			if (elasticsearchConfiguration.Enable)
			{
				do
				{
					await console.WriteAsync("ElasticSearch server endpoint (Include protocol and port, leave blank for http://127.0.0.1:9200): ", false, cancellationToken);
					var hostString = await console.ReadLineAsync(false, cancellationToken);
					if (String.IsNullOrWhiteSpace(hostString))
						hostString = "http://127.0.0.1:9200";

					if (Uri.TryCreate(hostString, UriKind.Absolute, out var host))
					{
						elasticsearchConfiguration.Host = host;
						break;
					}

					await console.WriteAsync("Invalid URI!", true, cancellationToken);
				}
				while (true);

				do
				{
					await console.WriteAsync("Enter Elasticsearch username: ", false, cancellationToken);
					elasticsearchConfiguration.Username = await console.ReadLineAsync(false, cancellationToken);
					if (!String.IsNullOrWhiteSpace(elasticsearchConfiguration.Username))
						break;
				}
				while (true);

				do
				{
					await console.WriteAsync("Enter password: ", false, cancellationToken);
					elasticsearchConfiguration.Password = await console.ReadLineAsync(true, cancellationToken);
					if (!String.IsNullOrWhiteSpace(elasticsearchConfiguration.Username))
						break;
				}
				while (true);
			}

			return elasticsearchConfiguration;
		}

		/// <summary>
		/// Prompts the user to create a <see cref="ControlPanelConfiguration"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the new <see cref="ControlPanelConfiguration"/>.</returns>
		async ValueTask<ControlPanelConfiguration> ConfigureControlPanel(CancellationToken cancellationToken)
		{
			var config = new ControlPanelConfiguration
			{
				Enable = await PromptYesNo("Enable the web control panel?", true, cancellationToken),
				AllowAnyOrigin = await PromptYesNo(
					"Allow web control panels hosted elsewhere to access the server? (Access-Control-Allow-Origin: *)",
					true,
					cancellationToken),
			};

			if (!config.AllowAnyOrigin)
			{
				await console.WriteAsync("Enter a comma seperated list of CORS allowed origins (optional): ", false, cancellationToken);
				var commaSeperatedOrigins = await console.ReadLineAsync(false, cancellationToken);
				if (!String.IsNullOrWhiteSpace(commaSeperatedOrigins))
				{
					var splits = commaSeperatedOrigins.Split(',');
					config.AllowedOrigins = new List<string>(splits.Select(x => x.Trim()));
				}
			}

			return config;
		}

		/// <summary>
		/// Prompts the user to create a <see cref="SwarmConfiguration"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the new <see cref="SwarmConfiguration"/>.</returns>
		async ValueTask<SwarmConfiguration?> ConfigureSwarm(CancellationToken cancellationToken)
		{
			var enable = await PromptYesNo("Enable swarm mode?", false, cancellationToken);
			if (!enable)
				return null;

			string identifer;
			do
			{
				await console.WriteAsync("Enter this server's identifer: ", false, cancellationToken);
				identifer = await console.ReadLineAsync(false, cancellationToken);
			}
			while (String.IsNullOrWhiteSpace(identifer));

			async ValueTask<Uri> ParseAddress(string question)
			{
				var first = true;
				Uri? address;
				do
				{
					if (first)
						first = false;
					else
						await console.WriteAsync("Invalid address!", true, cancellationToken);

					await console.WriteAsync(question, false, cancellationToken);
					var addressString = await console.ReadLineAsync(false, cancellationToken);
					if (Uri.TryCreate(addressString, UriKind.Absolute, out address)
						&& address.Scheme != Uri.UriSchemeHttp
						&& address.Scheme != Uri.UriSchemeHttps)
						address = null;
				}
				while (address == null);

				return address;
			}

			var address = await ParseAddress("Enter this server's INTERNAL http(s) address: ");
			var publicAddress = await ParseAddress("Enter this server's PUBLIC https(s) address: ");
			string privateKey;
			do
			{
				await console.WriteAsync("Enter the swarm private key: ", false, cancellationToken);
				privateKey = await console.ReadLineAsync(false, cancellationToken);
			}
			while (String.IsNullOrWhiteSpace(privateKey));

			var controller = await PromptYesNo("Is this server the swarm's controller? (y/n): ", null, cancellationToken);
			Uri? controllerAddress = null;
			if (!controller)
				controllerAddress = await ParseAddress("Enter the swarm controller's HTTP(S) address: ");

			return new SwarmConfiguration
			{
				Address = address,
				PublicAddress = publicAddress,
				ControllerAddress = controllerAddress,
				Identifier = identifer,
				PrivateKey = privateKey,
			};
		}

		/// <summary>
		/// Saves a given <see cref="Configuration"/> set to <paramref name="userConfigFileName"/>.
		/// </summary>
		/// <param name="userConfigFileName">The file to save the <see cref="Configuration"/> to.</param>
		/// <param name="apiSpec">The API <see cref="HostingSpecification"/> to save.</param>
		/// <param name="databaseConfiguration">The <see cref="DatabaseConfiguration"/> to save.</param>
		/// <param name="newGeneralConfiguration">The <see cref="GeneralConfiguration"/> to save.</param>
		/// <param name="fileLoggingConfiguration">The <see cref="FileLoggingConfiguration"/> to save.</param>
		/// <param name="elasticsearchConfiguration">The <see cref="ElasticsearchConfiguration"/> to save.</param>
		/// <param name="controlPanelConfiguration">The <see cref="ControlPanelConfiguration"/> to save.</param>
		/// <param name="swarmConfiguration">The <see cref="SwarmConfiguration"/> to save.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask SaveConfiguration(
			string userConfigFileName,
			HostingSpecification? apiSpec,
			DatabaseConfiguration databaseConfiguration,
			GeneralConfiguration newGeneralConfiguration,
			FileLoggingConfiguration? fileLoggingConfiguration,
			ElasticsearchConfiguration? elasticsearchConfiguration,
			ControlPanelConfiguration controlPanelConfiguration,
			SwarmConfiguration? swarmConfiguration,
			CancellationToken cancellationToken)
		{
			apiSpec ??= new HostingSpecification
			{
				Port = GeneralConfiguration.DefaultApiPort,
			};

			newGeneralConfiguration.ApiEndPoints = new List<HostingSpecification>
			{
				apiSpec,
			};
			newGeneralConfiguration.ConfigVersion = GeneralConfiguration.CurrentConfigVersion;
			var map = new Dictionary<string, object?>()
			{
				{ DatabaseConfiguration.Section, databaseConfiguration },
				{ GeneralConfiguration.Section, newGeneralConfiguration },
				{ FileLoggingConfiguration.Section, fileLoggingConfiguration },
				{ ElasticsearchConfiguration.Section, elasticsearchConfiguration },
				{ ControlPanelConfiguration.Section, controlPanelConfiguration },
				{
					SessionConfiguration.Section,
					new SessionConfiguration
					{
						BridgePort = apiSpec.Port,
					}
				},
				{ SwarmConfiguration.Section, swarmConfiguration },
			};

			var versionConverter = new VersionConverter();
			var builder = new SerializerBuilder()
				.WithTypeConverter(versionConverter);

			if (userConfigFileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
				builder.JsonCompatible();

			var serializer = new SerializerBuilder()
				.WithTypeConverter(versionConverter)
				.Build();

			var serializedYaml = serializer.Serialize(map);

			// big hack, but, prevent the default control panel channel from being overridden
			serializedYaml = serializedYaml.Replace(
				$"\n  {nameof(ControlPanelConfiguration.Channel)}: ",
				String.Empty,
				StringComparison.Ordinal)
				.Replace("\r", String.Empty, StringComparison.Ordinal);

			var configBytes = Encoding.UTF8.GetBytes(serializedYaml);

			try
			{
				await ioManager.WriteAllBytes(
					userConfigFileName,
					configBytes,
					cancellationToken);

				postSetupServices.ReloadRequired = true;
			}
			catch (Exception e) when (e is not OperationCanceledException)
			{
				await console.WriteAsync(e.Message, true, cancellationToken);
				await console.WriteAsync(null, true, cancellationToken);
				await console.WriteAsync("For your convienence, here's the yaml we tried to write out:", true, cancellationToken);
				await console.WriteAsync(null, true, cancellationToken);
				await console.WriteAsync(serializedYaml, true, cancellationToken);
				await console.WriteAsync(null, true, cancellationToken);
				await console.WriteAsync("Press any key to exit...", true, cancellationToken);
				await console.PressAnyKeyAsync(cancellationToken);
				throw new OperationCanceledException();
			}
		}

		/// <summary>
		/// Runs the <see cref="SetupWizard"/>.
		/// </summary>
		/// <param name="userConfigFileName">The path to the settings json to build.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask RunWizard(string userConfigFileName, CancellationToken cancellationToken)
		{
			// welcome message
			await console.WriteAsync($"Welcome to {Constants.CanonicalPackageName}!", true, cancellationToken);
			await console.WriteAsync("This wizard will help you configure your server.", true, cancellationToken);
			await console.WriteAsync("Note: Only the absolute basics will be covered. It is recommended that you configure your server manually using the README.md.", true, cancellationToken);

			var hostingSpec = await PromptForHostingSpec(cancellationToken);

			var databaseConfiguration = await ConfigureDatabase(cancellationToken);

			var newGeneralConfiguration = await ConfigureGeneral(cancellationToken);

			var fileLoggingConfiguration = await ConfigureLogging(cancellationToken);

			var elasticSearchConfiguration = await ConfigureElasticsearch(cancellationToken);

			var controlPanelConfiguration = await ConfigureControlPanel(cancellationToken);

			var swarmConfiguration = await ConfigureSwarm(cancellationToken);

			await console.WriteAsync(null, true, cancellationToken);
			await console.WriteAsync(String.Format(CultureInfo.InvariantCulture, "Configuration complete! Saving to {0}", userConfigFileName), true, cancellationToken);

			await SaveConfiguration(
				userConfigFileName,
				hostingSpec,
				databaseConfiguration,
				newGeneralConfiguration,
				fileLoggingConfiguration,
				elasticSearchConfiguration,
				controlPanelConfiguration,
				swarmConfiguration,
				cancellationToken);
		}

		/// <summary>
		/// Check if it should and run the <see cref="SetupWizard"/> if necessary.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask CheckRunWizard(CancellationToken cancellationToken)
		{
			var setupWizardMode = generalConfiguration.SetupWizardMode;
			if (setupWizardMode == SetupWizardMode.Never)
				return;

			var forceRun = setupWizardMode == SetupWizardMode.Force || setupWizardMode == SetupWizardMode.Only;
			if (!console.Available)
			{
				if (forceRun)
					throw new InvalidOperationException("Asked to run setup wizard with no console avaliable!");
				return;
			}

			var userConfigFileName = ioManager.ConcatPath(
				internalConfiguration.AppSettingsBasePath,
				$"{ServerFactory.AppSettings}.{hostingEnvironment.EnvironmentName}.yml");

			async Task HandleSetupCancel()
			{
				// DCTx2: Operation should always run
				await console.WriteAsync(String.Empty, true, default);
				await console.WriteAsync("Aborting setup!", true, default);
			}

			Task finalTask = Task.CompletedTask;
			string? originalConsoleTitle = null;
			void SetConsoleTitle()
			{
				if (originalConsoleTitle != null)
					return;

				originalConsoleTitle = console.Title;
				console.SetTitle($"{assemblyInformationProvider.VersionString} Setup Wizard");
			}

			// Link passed cancellationToken with cancel key press
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, console.CancelKeyPress))
			using ((cancellationToken = cts.Token).Register(() => finalTask = HandleSetupCancel()))
				try
				{
					var exists = await ioManager.FileExists(userConfigFileName, cancellationToken);
					if (!exists)
					{
						var legacyJsonFileName = $"appsettings.{hostingEnvironment.EnvironmentName}.json";
						exists = await ioManager.FileExists(legacyJsonFileName, cancellationToken);
						if (exists)
							userConfigFileName = legacyJsonFileName;
					}

					bool shouldRunBasedOnAutodetect;
					if (exists)
					{
						var bytes = await ioManager.ReadAllBytes(userConfigFileName, cancellationToken);
						var contents = Encoding.UTF8.GetString(bytes.Span);
						var lines = contents.Split('\n', StringSplitOptions.RemoveEmptyEntries);
						var existingConfigIsEmpty = lines
							.Select(line => line.Trim())
							.All(line => line[0] == '#' || line == "{}" || line.Length == 0);
						shouldRunBasedOnAutodetect = existingConfigIsEmpty;
					}
					else
						shouldRunBasedOnAutodetect = true;

					if (!shouldRunBasedOnAutodetect)
					{
						if (forceRun)
						{
							SetConsoleTitle();
							await console.WriteAsync(String.Format(CultureInfo.InvariantCulture, "The configuration settings are requesting the setup wizard be run, but you already appear to have a configuration file ({0})!", userConfigFileName), true, cancellationToken);

							forceRun = await PromptYesNo("Continue running setup wizard?", false, cancellationToken);
						}

						if (!forceRun)
							return;
					}

					SetConsoleTitle();

					if (!String.IsNullOrEmpty(internalConfiguration.MariaDBDefaultRootPassword))
					{
						// we can generate the whole thing.
						var csb = new MySqlConnectionStringBuilder
						{
							Server = "127.0.0.1",
							UserID = "root",
							Password = internalConfiguration.MariaDBDefaultRootPassword,
							Database = "tgs",
						};

						await SaveConfiguration(
							userConfigFileName,
							null,
							new DatabaseConfiguration
							{
								ConnectionString = csb.ConnectionString,
								DatabaseType = DatabaseType.MariaDB,
								ServerVersion = MasterVersionsAttribute.Instance.RawMariaDBRedistVersion,
							},
							new GeneralConfiguration(),
							null,
							null,
							new ControlPanelConfiguration
							{
								Enable = true,
								AllowAnyOrigin = true,
							},
							null,
							cancellationToken);
					}
					else
					{
						// flush the logs to prevent console conflicts
						await asyncDelayer.Delay(TimeSpan.FromSeconds(1), cancellationToken);

						await RunWizard(userConfigFileName, cancellationToken);
					}
				}
				finally
				{
					await finalTask;
					if (originalConsoleTitle != null)
						console.SetTitle(originalConsoleTitle);
				}
		}
	}
}
