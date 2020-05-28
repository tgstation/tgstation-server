using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database.Migrations;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// Backend abstract implementation of <see cref="IDatabaseContext"/>
	/// </summary>
#pragma warning disable CA1506 // TODO: Decomplexify
	abstract class DatabaseContext : DbContext, IDatabaseContext
	{
		/// <inheritdoc />
		public DatabaseType DatabaseType => DatabaseConfiguration.DatabaseType;

		/// <summary>
		/// The <see cref="User"/>s in the <see cref="DatabaseContext"/>.
		/// </summary>
		public DbSet<User> Users { get; set; }

		/// <summary>
		/// The <see cref="Instance"/>s in the <see cref="DatabaseContext"/>.
		/// </summary>
		public DbSet<Instance> Instances { get; set; }

		/// <summary>
		/// The <see cref="CompileJob"/>s in the <see cref="DatabaseContext"/>.
		/// </summary>
		public DbSet<CompileJob> CompileJobs { get; set; }

		/// <summary>
		/// The <see cref="RevisionInformation"/>s in the <see cref="DatabaseContext"/>.
		/// </summary>
		public DbSet<RevisionInformation> RevisionInformations { get; set; }

		/// <summary>
		/// The <see cref="Models.DreamMakerSettings"/> in the <see cref="DatabaseContext"/>.
		/// </summary>
		public DbSet<DreamMakerSettings> DreamMakerSettings { get; set; }

		/// <summary>
		/// The <see cref="ChatBot"/>s in the <see cref="DatabaseContext"/>.
		/// </summary>
		public DbSet<ChatBot> ChatBots { get; set; }

		/// <summary>
		/// The <see cref="Models.DreamDaemonSettings"/> in the <see cref="DatabaseContext"/>.
		/// </summary>
		public DbSet<DreamDaemonSettings> DreamDaemonSettings { get; set; }

		/// <summary>
		/// The <see cref="Models.RepositorySettings"/> in the <see cref="DatabaseContext"/>.
		/// </summary>
		public DbSet<RepositorySettings> RepositorySettings { get; set; }

		/// <summary>
		/// The <see cref="InstanceUser"/>s in the <see cref="DatabaseContext"/>.
		/// </summary>
		public DbSet<InstanceUser> InstanceUsers { get; set; }

		/// <summary>
		/// The <see cref="ChatChannel"/>s in the <see cref="DatabaseContext"/>.
		/// </summary>
		public DbSet<ChatChannel> ChatChannels { get; set; }

		/// <summary>
		/// The <see cref="Job"/>s in the <see cref="DatabaseContext"/>.
		/// </summary>
		public DbSet<Job> Jobs { get; set; }

		/// <summary>
		/// The <see cref="ReattachInformation"/>s in the <see cref="DatabaseContext"/>.
		/// </summary>
		public DbSet<ReattachInformation> ReattachInformations { get; set; }

		/// <summary>
		/// The <see cref="DualReattachInformation"/>s in the <see cref="DatabaseContext"/>.
		/// </summary>
		public DbSet<DualReattachInformation> WatchdogReattachInformations { get; set; }

		/// <summary>
		/// The <see cref="TestMerge"/>s in the <see cref="DatabaseContext"/>
		/// </summary>
		public DbSet<TestMerge> TestMerges { get; set; }

		/// <summary>
		/// The <see cref="RevInfoTestMerge"/>s om the <see cref="DatabaseContext"/>
		/// </summary>
		public DbSet<RevInfoTestMerge> RevInfoTestMerges { get; set; }

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="DatabaseContext"/>
		/// </summary>
		protected ILogger Logger { get; }

		/// <summary>
		/// The <see cref="DatabaseConfiguration"/> for the <see cref="DatabaseContext"/>
		/// </summary>
		protected DatabaseConfiguration DatabaseConfiguration { get; }

		/// <inheritdoc />
		IDatabaseCollection<User> IDatabaseContext.Users => usersCollection;

		/// <inheritdoc />
		IDatabaseCollection<Instance> IDatabaseContext.Instances => instancesCollection;

		/// <inheritdoc />
		IDatabaseCollection<InstanceUser> IDatabaseContext.InstanceUsers => instanceUsersCollection;

		/// <inheritdoc />
		IDatabaseCollection<Job> IDatabaseContext.Jobs => jobsCollection;

		/// <inheritdoc />
		IDatabaseCollection<CompileJob> IDatabaseContext.CompileJobs => compileJobsCollection;

		/// <inheritdoc />
		IDatabaseCollection<RevisionInformation> IDatabaseContext.RevisionInformations => revisionInformationsCollection;

		/// <inheritdoc />
		IDatabaseCollection<DreamMakerSettings> IDatabaseContext.DreamMakerSettings => dreamMakerSettingsCollection;

		/// <inheritdoc />
		IDatabaseCollection<DreamDaemonSettings> IDatabaseContext.DreamDaemonSettings => dreamDaemonSettingsCollection;

		/// <inheritdoc />
		IDatabaseCollection<ChatBot> IDatabaseContext.ChatBots => chatBotsCollection;

		/// <inheritdoc />
		IDatabaseCollection<ChatChannel> IDatabaseContext.ChatChannels => chatChannelsCollection;

		/// <inheritdoc />
		IDatabaseCollection<RepositorySettings> IDatabaseContext.RepositorySettings => repositorySettingsCollection;

		/// <inheritdoc />
		IDatabaseCollection<ReattachInformation> IDatabaseContext.ReattachInformations => reattachInformationsCollection;

		/// <inheritdoc />
		IDatabaseCollection<DualReattachInformation> IDatabaseContext.WatchdogReattachInformations => watchdogReattachInformationsCollection;

		/// <summary>
		/// The <see cref="IDatabaseSeeder"/> for the <see cref="DatabaseContext"/>
		/// </summary>
		readonly IDatabaseSeeder databaseSeeder;

		/// <summary>
		/// Backing field for <see cref="IDatabaseContext.Users"/>.
		/// </summary>
		readonly IDatabaseCollection<User> usersCollection;

		/// <summary>
		/// Backing field for <see cref="IDatabaseContext.Instances"/>.
		/// </summary>
		readonly IDatabaseCollection<Instance> instancesCollection;

		/// <summary>
		/// Backing field for <see cref="IDatabaseContext.CompileJobs"/>.
		/// </summary>
		readonly IDatabaseCollection<CompileJob> compileJobsCollection;

		/// <summary>
		/// Backing field for <see cref="IDatabaseContext.InstanceUsers"/>.
		/// </summary>
		readonly IDatabaseCollection<InstanceUser> instanceUsersCollection;

		/// <summary>
		/// Backing field for <see cref="IDatabaseContext.Jobs"/>.
		/// </summary>
		readonly IDatabaseCollection<Job> jobsCollection;

		/// <summary>
		/// Backing field for <see cref="IDatabaseContext.RevisionInformations"/>.
		/// </summary>
		readonly IDatabaseCollection<RevisionInformation> revisionInformationsCollection;

		/// <summary>
		/// Backing field for <see cref="IDatabaseContext.DreamMakerSettings"/>.
		/// </summary>
		readonly IDatabaseCollection<DreamMakerSettings> dreamMakerSettingsCollection;

		/// <summary>
		/// Backing field for <see cref="IDatabaseContext.DreamDaemonSettings"/>.
		/// </summary>
		readonly IDatabaseCollection<DreamDaemonSettings> dreamDaemonSettingsCollection;

		/// <summary>
		/// Backing field for <see cref="IDatabaseContext.ChatBots"/>.
		/// </summary>
		readonly IDatabaseCollection<ChatBot> chatBotsCollection;

		/// <summary>
		/// Backing field for <see cref="IDatabaseContext.ChatChannels"/>.
		/// </summary>
		readonly IDatabaseCollection<ChatChannel> chatChannelsCollection;

		/// <summary>
		/// Backing field for <see cref="IDatabaseContext.RepositorySettings"/>.
		/// </summary>
		readonly IDatabaseCollection<RepositorySettings> repositorySettingsCollection;

		/// <summary>
		/// Backing field for <see cref="IDatabaseContext.ReattachInformations"/>.
		/// </summary>
		readonly IDatabaseCollection<ReattachInformation> reattachInformationsCollection;

		/// <summary>
		/// Backing field for <see cref="IDatabaseContext.WatchdogReattachInformations"/>.
		/// </summary>
		readonly IDatabaseCollection<DualReattachInformation> watchdogReattachInformationsCollection;

		/// <summary>
		/// Construct a <see cref="DatabaseContext"/>
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions"/> for the <see cref="DatabaseContext"/>.</param>
		/// <param name="databaseConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="DatabaseConfiguration"/></param>
		/// <param name="databaseSeeder">The value of <see cref="databaseSeeder"/></param>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		public DatabaseContext(DbContextOptions dbContextOptions, IOptions<DatabaseConfiguration> databaseConfigurationOptions, IDatabaseSeeder databaseSeeder, ILogger logger) : base(dbContextOptions)
		{
			DatabaseConfiguration = databaseConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(databaseConfigurationOptions));
			this.databaseSeeder = databaseSeeder ?? throw new ArgumentNullException(nameof(databaseSeeder));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));

			usersCollection = new DatabaseCollection<User>(Users);
			instancesCollection = new DatabaseCollection<Instance>(Instances);
			instanceUsersCollection = new DatabaseCollection<InstanceUser>(InstanceUsers);
			compileJobsCollection = new DatabaseCollection<CompileJob>(CompileJobs);
			repositorySettingsCollection = new DatabaseCollection<RepositorySettings>(RepositorySettings);
			dreamMakerSettingsCollection = new DatabaseCollection<DreamMakerSettings>(DreamMakerSettings);
			dreamDaemonSettingsCollection = new DatabaseCollection<DreamDaemonSettings>(DreamDaemonSettings);
			chatBotsCollection = new DatabaseCollection<ChatBot>(ChatBots);
			chatChannelsCollection = new DatabaseCollection<ChatChannel>(ChatChannels);
			revisionInformationsCollection = new DatabaseCollection<RevisionInformation>(RevisionInformations);
			jobsCollection = new DatabaseCollection<Job>(Jobs);
			reattachInformationsCollection = new DatabaseCollection<ReattachInformation>(ReattachInformations);
			watchdogReattachInformationsCollection = new DatabaseCollection<DualReattachInformation>(WatchdogReattachInformations);
		}

		/// <inheritdoc />
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// Setup our more complex database relations
			Logger.LogTrace("Building entity framework context...");
			base.OnModelCreating(modelBuilder);

			var userModel = modelBuilder.Entity<User>();
			userModel.HasIndex(x => x.CanonicalName).IsUnique();
			userModel.HasIndex(x => x.SystemIdentifier).IsUnique();
			userModel.HasMany(x => x.TestMerges).WithOne(x => x.MergedBy).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<InstanceUser>().HasIndex(x => new { x.UserId, x.InstanceId }).IsUnique();

			var revInfo = modelBuilder.Entity<RevisionInformation>();
			revInfo.HasMany(x => x.ActiveTestMerges).WithOne(x => x.RevisionInformation).OnDelete(DeleteBehavior.Cascade);
			revInfo.HasOne(x => x.PrimaryTestMerge).WithOne(x => x.PrimaryRevisionInformation).OnDelete(DeleteBehavior.Cascade);
			revInfo.HasIndex(x => new { x.InstanceId, x.CommitSha }).IsUnique();

			// IMPORTANT: When an instance is deleted (detached) it cascades into the maze of revinfo/testmerge/ritm/compilejob/job/ri relations
			// This maze starts at revInfo and jobs
			// jobs takes care of deleting compile jobs and ris
			// rev info takes care of the rest
			// Break the link here so the db doesn't shit itself complaining about cascading deletes
			// EF will handle making the right query to destroy everything
			revInfo.HasMany(x => x.CompileJobs).WithOne(x => x.RevisionInformation).OnDelete(DeleteBehavior.ClientNoAction);

			// Also break the link between ritm and testmerge so it doesn't cycle in a triangle with rev info
			modelBuilder.Entity<TestMerge>().HasMany(x => x.RevisonInformations).WithOne(x => x.TestMerge).OnDelete(DeleteBehavior.ClientNoAction);

			var compileJob = modelBuilder.Entity<CompileJob>();
			compileJob.HasIndex(x => x.DirectoryName);
			compileJob.HasOne(x => x.Job).WithOne().OnDelete(DeleteBehavior.Cascade);

			var chatChannel = modelBuilder.Entity<ChatChannel>();
			chatChannel.HasIndex(x => new { x.ChatSettingsId, x.IrcChannel }).IsUnique();
			chatChannel.HasIndex(x => new { x.ChatSettingsId, x.DiscordChannelId }).IsUnique();
			chatChannel.HasOne(x => x.ChatSettings).WithMany(x => x.Channels).HasForeignKey(x => x.ChatSettingsId).OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<ChatBot>().HasIndex(x => new { x.InstanceId, x.Name }).IsUnique();

			var instanceModel = modelBuilder.Entity<Instance>();
			instanceModel.HasIndex(x => x.Path).IsUnique();
			instanceModel.HasMany(x => x.ChatSettings).WithOne(x => x.Instance).OnDelete(DeleteBehavior.Cascade);
			instanceModel.HasOne(x => x.DreamDaemonSettings).WithOne(x => x.Instance).OnDelete(DeleteBehavior.Cascade);
			instanceModel.HasOne(x => x.DreamMakerSettings).WithOne(x => x.Instance).OnDelete(DeleteBehavior.Cascade);
			instanceModel.HasOne(x => x.RepositorySettings).WithOne(x => x.Instance).OnDelete(DeleteBehavior.Cascade);
			instanceModel.HasMany(x => x.RevisionInformations).WithOne(x => x.Instance).OnDelete(DeleteBehavior.Cascade);
			instanceModel.HasMany(x => x.InstanceUsers).WithOne(x => x.Instance).OnDelete(DeleteBehavior.Cascade);
			instanceModel.HasMany(x => x.Jobs).WithOne(x => x.Instance).OnDelete(DeleteBehavior.Cascade);
			instanceModel.HasOne(x => x.WatchdogReattachInformation).WithOne().OnDelete(DeleteBehavior.Cascade);
		}

		/// <inheritdoc />
		public async Task Initialize(CancellationToken cancellationToken)
		{
			ValidateDatabaseType();

			if (DatabaseConfiguration.DropDatabase)
			{
				Logger.LogCritical("DropDatabase configuration option set! Dropping any existing database...");
				await Database.EnsureDeletedAsync(cancellationToken).ConfigureAwait(false);
			}

			var migrations = await Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);
			var wasEmpty = !migrations.Any();

			if (wasEmpty || (await Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
			{
				Logger.LogInformation("Migrating database...");
				await Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
			}
			else
				Logger.LogDebug("No migrations to apply.");

			wasEmpty |= (await Users.AsQueryable().CountAsync(cancellationToken).ConfigureAwait(false)) == 0;

			if (wasEmpty)
			{
				Logger.LogInformation("Seeding database...");
				await databaseSeeder.SeedDatabase(this, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				if (DatabaseConfiguration.ResetAdminPassword)
				{
					Logger.LogWarning("Enabling and resetting admin password due to configuration!");
					await databaseSeeder.ResetAdminPassword(this, cancellationToken).ConfigureAwait(false);
				}

				await databaseSeeder.SanitizeDatabase(this, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public Task Save(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);

		/// <inheritdoc />
		public async Task SchemaDowngradeForServerVersion(Version version, CancellationToken cancellationToken)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));
			if (version < new Version(4, 0))
				throw new ArgumentOutOfRangeException(nameof(version), version, "Not a valid V4 version!");

			// Update this with new migrations as they are made
			string targetMigration = null;

			if (DatabaseType == DatabaseType.PostgresSql && version < new Version(4, 3, 0))
				throw new NotSupportedException("Cannot migrate below version 4.3.0 with PostgresSql!");

			if (version < new Version(4, 1, 0))
				throw new NotSupportedException("Cannot migrate below version 4.1.0!");

			if (version < new Version(4, 2, 0))
				targetMigration = DatabaseType == DatabaseType.Sqlite ? nameof(SLRebuild) : nameof(MSFixCascadingDelete);

			if (targetMigration == null)
			{
				Logger.LogDebug("No down migration required.");
				return;
			}

			string migrationSubstitution;
			switch (DatabaseType)
			{
				case DatabaseType.SqlServer:
					// already setup
					migrationSubstitution = null;
					break;
				case DatabaseType.MySql:
				case DatabaseType.MariaDB:
					migrationSubstitution = "MY{0}";
					break;
				case DatabaseType.Sqlite:
					migrationSubstitution = "SL{0}";
					break;
				case DatabaseType.PostgresSql:
					migrationSubstitution = "PG{0}";
					break;
				default:
					throw new InvalidOperationException($"Invalid DatabaseType: {DatabaseType}");
			}

			if (migrationSubstitution != null)
				targetMigration = String.Format(CultureInfo.InvariantCulture, migrationSubstitution, targetMigration.Substring(2));

			// even though it clearly implements it in the DatabaseFacade definition this won't work without casting (╯ಠ益ಠ)╯︵ ┻━┻
			var dbServiceProvider = ((IInfrastructure<IServiceProvider>)Database).Instance;
			var migrator = dbServiceProvider.GetRequiredService<IMigrator>();

			Logger.LogInformation("Migrating down to version {0}. Target: {1}", version, targetMigration);
			try
			{
				await migrator.MigrateAsync(targetMigration, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				Logger.LogCritical("Failed to migrate! Exception: {0}", e);
			}
		}

		/// <summary>
		/// Ensure the <see cref="DatabaseType"/> is correct for the <see cref="DatabaseContext"/>.
		/// </summary>
		protected abstract void ValidateDatabaseType();
	}
}
