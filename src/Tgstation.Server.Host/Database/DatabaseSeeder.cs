using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;
using Z.EntityFramework.Plus;

namespace Tgstation.Server.Host.Database
{
	/// <inheritdoc />
	sealed class DatabaseSeeder : IDatabaseSeeder
	{
		/// <summary>
		/// The <see cref="ICryptographySuite"/> for the <see cref="DatabaseSeeder"/>
		/// </summary>
		readonly ICryptographySuite cryptographySuite;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="DatabaseSeeder"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="ILogger"/> used for <see cref="IDatabaseContext"/>s.
		/// </summary>
		readonly ILogger<DatabaseContext> databaseLogger;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="DatabaseSeeder"/>.
		/// </summary>
		readonly ILogger<DatabaseSeeder> logger;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="DatabaseSeeder"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// The <see cref="DatabaseConfiguration"/> for the <see cref="DatabaseSeeder"/>.
		/// </summary>
		readonly DatabaseConfiguration databaseConfiguration;

		/// <summary>
		/// Construct a <see cref="DatabaseSeeder"/>
		/// </summary>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/></param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="databaseConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="databaseConfiguration"/>.</param>
		/// <param name="databaseLogger">The value of <see cref="databaseLogger"/></param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public DatabaseSeeder(
			ICryptographySuite cryptographySuite,
			IPlatformIdentifier platformIdentifier,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<DatabaseConfiguration> databaseConfigurationOptions,
			ILogger<DatabaseContext> databaseLogger,
			ILogger<DatabaseSeeder> logger)
		{
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			databaseConfiguration = databaseConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(databaseConfigurationOptions));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			this.databaseLogger = databaseLogger ?? throw new ArgumentNullException(nameof(databaseLogger));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Add a default admin <see cref="User"/> to a given <paramref name="databaseContext"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to add an admin <see cref="User"/> to</param>
		void SeedAdminUser(IDatabaseContext databaseContext)
		{
			var admin = new User
			{
				AdministrationRights = RightsHelper.AllRights<AdministrationRights>(),
				CreatedAt = DateTimeOffset.Now,
				InstanceManagerRights = RightsHelper.AllRights<InstanceManagerRights>(),
				Name = Api.Models.User.AdminName,
				CanonicalName = User.CanonicalizeName(Api.Models.User.AdminName),
				Enabled = true,
			};
			cryptographySuite.SetUserPassword(admin, Api.Models.User.DefaultAdminPassword, true);
			databaseContext.Users.Add(admin);
		}

		/// <summary>
		/// Initially seed a given <paramref name="databaseContext"/>
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to seed</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task SeedDatabase(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			SeedAdminUser(databaseContext);
			await databaseContext.Save(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Correct invalid database data caused by previous versions.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to sanitize.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task SanitizeDatabase(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			var admin = await GetAdminUser(databaseContext, cancellationToken).ConfigureAwait(false);
			if (admin != null)
			{
				// Fix the issue with ulong enums
				// https://github.com/tgstation/tgstation-server/commit/db341d43b3dab74fe3681f5172ca9bfeaafa6b6d#diff-09f06ec4584665cf89bb77b97f5ccfb9R36-R39
				// https://github.com/JamesNK/Newtonsoft.Json/issues/2301
				admin.AdministrationRights &= RightsHelper.AllRights<AdministrationRights>();
				admin.InstanceManagerRights &= RightsHelper.AllRights<InstanceManagerRights>();
			}

			if (platformIdentifier.IsWindows)
			{
				// normalize backslashes to forward slashes
				var allInstances = await databaseContext
					.Instances
					.AsQueryable()
					.ToListAsync(cancellationToken)
					.ConfigureAwait(false);
				foreach (var instance in allInstances)
					instance.Path = instance.Path.Replace('\\', '/');
			}

			// Update settings from config
			var rowsUpdated = await databaseContext
				.DreamDaemonSettings
				.AsQueryable()
				.Where(x => x.TopicRequestTimeout == 0)
				.UpdateAsync(
					settings => new DreamDaemonSettings { TopicRequestTimeout = generalConfiguration.ByondTopicTimeout },
					cancellationToken)
				.ConfigureAwait(false);

			if (rowsUpdated > 0)
				logger.LogInformation(
					"Updated {0} instances to use database backed BYOND topic timeouts from configuration setting of {1}",
					rowsUpdated,
					generalConfiguration.ByondTopicTimeout);

			await databaseContext.Save(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Changes the admin password in <see cref="IDatabaseContext"/> back to it's default and enables the account
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to reset the admin password for</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task ResetAdminPassword(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			var admin = await GetAdminUser(databaseContext, cancellationToken).ConfigureAwait(false);
			if (admin != null)
			{
				admin.Enabled = true;
				cryptographySuite.SetUserPassword(admin, Api.Models.User.DefaultAdminPassword, false);
			}

			await databaseContext.Save(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Get or create the admin <see cref="User"/>.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to use.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the admin <see cref="User"/> or <see langword="null"/>. If <see langword="null"/>, <see cref="IDatabaseContext.Save(CancellationToken)"/> must be called on <paramref name="databaseContext"/>.</returns>
		async Task<User> GetAdminUser(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			var admin = await databaseContext
				.Users
				.AsQueryable()
				.Where(x => x.CanonicalName == User.CanonicalizeName(Api.Models.User.AdminName))
				.FirstOrDefaultAsync(cancellationToken)
				.ConfigureAwait(false);
			if (admin == default)
				SeedAdminUser(databaseContext);

			return admin;
		}

		/// <inheritdoc />
		public async Task Initialize(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			if (databaseContext == null)
				throw new ArgumentNullException(nameof(databaseContext));

			if (databaseConfiguration.DropDatabase)
			{
				logger.LogCritical("DropDatabase configuration option set! Dropping any existing database...");
				await databaseContext.Drop(cancellationToken).ConfigureAwait(false);
			}

			var wasEmpty = await databaseContext.Migrate(databaseLogger, cancellationToken).ConfigureAwait(false);
			if (wasEmpty)
			{
				logger.LogInformation("Seeding database...");
				await SeedDatabase(databaseContext, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				if (databaseConfiguration.ResetAdminPassword)
				{
					logger.LogWarning("Enabling and resetting admin password due to configuration!");
					await ResetAdminPassword(databaseContext, cancellationToken).ConfigureAwait(false);
				}

				await SanitizeDatabase(databaseContext, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public Task Downgrade(IDatabaseContext databaseContext, Version downgradeVersion, CancellationToken cancellationToken)
		{
			if (databaseContext == null)
				throw new ArgumentNullException(nameof(databaseContext));
			if (downgradeVersion == null)
				throw new ArgumentNullException(nameof(downgradeVersion));

			return databaseContext.SchemaDowngradeForServerVersion(databaseLogger, downgradeVersion, databaseConfiguration.DatabaseType, cancellationToken);
		}
	}
}
