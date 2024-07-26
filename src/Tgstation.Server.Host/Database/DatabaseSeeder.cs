using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api;
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
		/// The <see cref="ICryptographySuite"/> for the <see cref="DatabaseSeeder"/>.
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
		/// Add a default system <see cref="User"/> to a given <paramref name="databaseContext"/>.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to add a system <see cref="User"/> to.</param>
		/// <param name="tgsUser">An existing <see cref="User"/>, if any.</param>
		/// <returns>The created system <see cref="User"/>.</returns>
		static User SeedSystemUser(IDatabaseContext databaseContext, User? tgsUser = null)
		{
			bool alreadyExists = tgsUser != null;
			tgsUser ??= new User()
			{
				CreatedAt = DateTimeOffset.UtcNow,
				CanonicalName = User.CanonicalizeName(User.TgsSystemUserName),
			};

			// intentionally not giving a group or permissionset
			tgsUser.Name = User.TgsSystemUserName;
			tgsUser.PasswordHash = "_"; // This can't be hashed
			tgsUser.Enabled = false;

			if (!alreadyExists)
				databaseContext.Users.Add(tgsUser);
			return tgsUser;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseSeeder"/> class.
		/// </summary>
		/// <param name="cryptographySuite">The value of <see cref="cryptographySuite"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="databaseConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="databaseConfiguration"/>.</param>
		/// <param name="databaseLogger">The value of <see cref="databaseLogger"/>.</param>
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

		/// <inheritdoc />
		public async ValueTask Initialize(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(databaseContext);

			if (databaseConfiguration.DropDatabase)
			{
				logger.LogCritical("DropDatabase configuration option set! Dropping any existing database...");
				await databaseContext.Drop(cancellationToken);
			}

			var wasEmpty = await databaseContext.Migrate(databaseLogger, cancellationToken);
			if (wasEmpty)
			{
				logger.LogInformation("Seeding database...");
				await SeedDatabase(databaseContext, cancellationToken);
			}
			else
			{
				if (databaseConfiguration.ResetAdminPassword)
				{
					logger.LogWarning("Enabling and resetting admin password due to configuration!");
					await ResetAdminPassword(databaseContext, cancellationToken);
				}

				await SanitizeDatabase(databaseContext, cancellationToken);
			}
		}

		/// <inheritdoc />
		public ValueTask Downgrade(IDatabaseContext databaseContext, Version downgradeVersion, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(databaseContext);
			ArgumentNullException.ThrowIfNull(downgradeVersion);

			return databaseContext.SchemaDowngradeForServerVersion(
				databaseLogger,
				downgradeVersion,
				databaseConfiguration.DatabaseType,
				cancellationToken);
		}

		/// <summary>
		/// Add a default admin <see cref="User"/> to a given <paramref name="databaseContext"/>.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to add an admin <see cref="User"/> to.</param>
		/// <returns>The created admin <see cref="User"/>.</returns>
		User SeedAdminUser(IDatabaseContext databaseContext)
		{
			var admin = new User
			{
				PermissionSet = new PermissionSet
				{
					AdministrationRights = RightsHelper.AllRights<AdministrationRights>(),
					InstanceManagerRights = RightsHelper.AllRights<InstanceManagerRights>(),
				},
				CreatedAt = DateTimeOffset.UtcNow,
				Name = DefaultCredentials.AdminUserName,
				CanonicalName = User.CanonicalizeName(DefaultCredentials.AdminUserName),
				Enabled = true,
			};
			cryptographySuite.SetUserPassword(admin, DefaultCredentials.DefaultAdminUserPassword, true);
			databaseContext.Users.Add(admin);
			return admin;
		}

		/// <summary>
		/// Initially seed a given <paramref name="databaseContext"/>.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to seed.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask SeedDatabase(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			var adminUser = SeedAdminUser(databaseContext);

			// Save here because we want admin to have the first DB Id
			// The system user isn't shown in the API except by references in the admin user and jobs
			await databaseContext.Save(cancellationToken);
			var tgsUser = SeedSystemUser(databaseContext);
			adminUser.CreatedBy = tgsUser;

			await databaseContext.Save(cancellationToken);
		}

		/// <summary>
		/// Correct invalid database data caused by previous versions (NOT user fuckery).
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to sanitize.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask SanitizeDatabase(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			var admin = await GetAdminUser(databaseContext, cancellationToken);
			if (admin != null)
			{
				if (admin.PermissionSet != null)
				{
					// Fix the issue with ulong enums
					// https://github.com/tgstation/tgstation-server/commit/db341d43b3dab74fe3681f5172ca9bfeaafa6b6d#diff-09f06ec4584665cf89bb77b97f5ccfb9R36-R39
					// https://github.com/JamesNK/Newtonsoft.Json/issues/2301
					admin.PermissionSet.AdministrationRights &= RightsHelper.AllRights<AdministrationRights>();
					admin.PermissionSet.InstanceManagerRights &= RightsHelper.AllRights<InstanceManagerRights>();
				}

				if (admin.CreatedBy == null)
				{
					var tgsUser = await databaseContext
						.Users
						.AsQueryable()
						.Where(x => x.CanonicalName == User.CanonicalizeName(User.TgsSystemUserName))
						.FirstOrDefaultAsync(cancellationToken);

					if (tgsUser != null)
						logger.LogError(
							"A user named TGS (Canonically) exists but isn't marked as the admin's creator. This may be because it was created manually. This user is going to be adapted to use as the starter of system jobs.");

					tgsUser = SeedSystemUser(databaseContext, tgsUser);
					admin.CreatedBy = tgsUser;
				}
			}

			// normalize backslashes to forward slashes
			var allInstances = await databaseContext
				.Instances
				.AsQueryable()
				.ToListAsync(cancellationToken);
			foreach (var instance in allInstances)
				instance.Path = platformIdentifier.NormalizePath(instance.Path!.Replace('\\', '/'));

			if (generalConfiguration.ByondTopicTimeout != 0)
			{
				var ids = await databaseContext
					.DreamDaemonSettings
					.AsQueryable()
					.Where(x => x.TopicRequestTimeout == 0)
					.Select(x => x.Id)
					.ToListAsync(cancellationToken);

				var rowsUpdated = ids.Count;
				foreach (var id in ids)
				{
					var newDDSettings = new DreamDaemonSettings
					{
						Id = id,
					};

					databaseContext.DreamDaemonSettings.Attach(newDDSettings);
					newDDSettings.TopicRequestTimeout = generalConfiguration.ByondTopicTimeout;
				}

				if (rowsUpdated > 0)
					logger.LogInformation(
						"Updated {count} instances to use database backed BYOND topic timeouts from configuration setting of {timeout}",
						rowsUpdated,
						generalConfiguration.ByondTopicTimeout);
			}

			await databaseContext.Save(cancellationToken);
		}

		/// <summary>
		/// Changes the admin password in <see cref="IDatabaseContext"/> back to it's default, enables the account, and gives it <see cref="AdministrationRights.WriteUsers"/> access.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to reset the admin password for.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		async ValueTask ResetAdminPassword(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			var admin = await GetAdminUser(databaseContext, cancellationToken);
			if (admin != null)
			{
				admin.Enabled = true;

				// force the user out of any groups
				if (admin.PermissionSet == null)
				{
					admin.Group = null;
					admin.GroupId = null;
					admin.PermissionSet = new PermissionSet
					{
						InstanceManagerRights = InstanceManagerRights.None,
						AdministrationRights = AdministrationRights.WriteUsers,
					};
				}
				else
					admin.PermissionSet.AdministrationRights |= AdministrationRights.WriteUsers;
				cryptographySuite.SetUserPassword(admin, DefaultCredentials.DefaultAdminUserPassword, false);
			}

			await databaseContext.Save(cancellationToken);
		}

		/// <summary>
		/// Get or create the admin <see cref="User"/>.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to use.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the admin <see cref="User"/> or <see langword="null"/>. If <see langword="null"/>, <see cref="IDatabaseContext.Save(CancellationToken)"/> must be called on <paramref name="databaseContext"/>.</returns>
		async ValueTask<User?> GetAdminUser(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			var admin = await databaseContext
				.Users
				.AsQueryable()
				.Where(x => x.CanonicalName == User.CanonicalizeName(DefaultCredentials.AdminUserName))
				.Include(x => x.CreatedBy)
				.Include(x => x.PermissionSet)
				.Include(x => x.Group)
				.FirstOrDefaultAsync(cancellationToken);
			if (admin == default)
				SeedAdminUser(databaseContext);

			return admin;
		}
	}
}
