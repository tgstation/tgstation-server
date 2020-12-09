using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
	public abstract class DatabaseContext : DbContext, IDatabaseContext
	{
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
		/// The <see cref="TestMerge"/>s in the <see cref="DatabaseContext"/>
		/// </summary>
		public DbSet<TestMerge> TestMerges { get; set; }

		/// <summary>
		/// The <see cref="RevInfoTestMerge"/>s om the <see cref="DatabaseContext"/>
		/// </summary>
		public DbSet<RevInfoTestMerge> RevInfoTestMerges { get; set; }

		/// <summary>
		/// The <see cref="DeleteBehavior"/> for the <see cref="CompileJob"/>/<see cref="RevisionInformation"/> foreign key.
		/// </summary>
		protected virtual DeleteBehavior RevInfoCompileJobDeleteBehavior => DeleteBehavior.ClientNoAction;

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
		/// Gets the configure action for a given <typeparamref name="TDatabaseContext"/>.
		/// </summary>
		/// <typeparam name="TDatabaseContext">The <see cref="DatabaseContext"/> parent class to configure with.</typeparam>
		/// <returns>A configure <see cref="Action{T1, T2}"/>.</returns>
		public static Action<DbContextOptionsBuilder, DatabaseConfiguration> GetConfigureAction<TDatabaseContext>()
			where TDatabaseContext : DatabaseContext
		{
			// HACK HACK HACK HACK HACK
			const string ConfigureMethodName = nameof(SqlServerDatabaseContext.ConfigureWith);
			var configureFunction = typeof(TDatabaseContext).GetMethod(
				ConfigureMethodName,
				BindingFlags.Public | BindingFlags.Static);

			if (configureFunction == null)
				throw new InvalidOperationException($"Context type {typeof(TDatabaseContext).FullName} missing static {ConfigureMethodName} function!");

			return (optionsBuilder, config) => configureFunction.Invoke(null, new object[] { optionsBuilder, config });
		}

		/// <summary>
		/// Construct a <see cref="DatabaseContext"/>
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions"/> for the <see cref="DatabaseContext"/>.</param>
		public DatabaseContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
		{
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
		}

		/// <inheritdoc />
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			if (modelBuilder == null)
				throw new ArgumentNullException(nameof(modelBuilder));

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
			// UPDATE: I fuck with this constantly in hopes of eliminating FK issues on instance detack
			revInfo.HasMany(x => x.CompileJobs).WithOne(x => x.RevisionInformation).OnDelete(RevInfoCompileJobDeleteBehavior);

			// Also break the link between ritm and testmerge so it doesn't cycle in a triangle with rev info
			modelBuilder.Entity<TestMerge>().HasMany(x => x.RevisonInformations).WithOne(x => x.TestMerge).OnDelete(DeleteBehavior.ClientNoAction);

			var compileJob = modelBuilder.Entity<CompileJob>();
			compileJob.HasIndex(x => x.DirectoryName);
			compileJob.HasOne(x => x.Job).WithOne().OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<ReattachInformation>().HasOne(x => x.CompileJob).WithMany().OnDelete(DeleteBehavior.Cascade);

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
		}

		/// <inheritdoc />
		public Task Save(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);

		/// <inheritdoc />
		public Task Drop(CancellationToken cancellationToken) => Database.EnsureDeletedAsync(cancellationToken);

		/// <inheritdoc />
		public async Task<bool> Migrate(ILogger<DatabaseContext> logger, CancellationToken cancellationToken)
		{
			if (logger == null)
				throw new ArgumentNullException(nameof(logger));
			var migrations = await Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);
			var wasEmpty = !migrations.Any();

			if (wasEmpty || (await Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
			{
				logger.LogInformation("Migrating database...");
				await Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
			}
			else
				logger.LogDebug("No migrations to apply");

			wasEmpty |= (await Users.AsQueryable().CountAsync(cancellationToken).ConfigureAwait(false)) == 0;

			return wasEmpty;
		}

#if DEBUG
		/// <summary>
		/// Used by unit tests to remind us to setup the correct MSSQL migration downgrades.
		/// </summary>
		public static readonly Type MSLatestMigration = typeof(MSAddAdditionalDDParameters);

		/// <summary>
		/// Used by unit tests to remind us to setup the correct MSSQL migration downgrades.
		/// </summary>
		public static readonly Type MYLatestMigration = typeof(MYAddAdditionalDDParameters);

		/// <summary>
		/// Used by unit tests to remind us to setup the correct MSSQL migration downgrades.
		/// </summary>
		public static readonly Type PGLatestMigration = typeof(PGAddAdditionalDDParameters);

		/// <summary>
		/// Used by unit tests to remind us to setup the correct MSSQL migration downgrades.
		/// </summary>
		public static readonly Type SLLatestMigration = typeof(SLAddAdditionalDDParameters);
#endif

		/// <inheritdoc />
		public async Task SchemaDowngradeForServerVersion(
			ILogger<DatabaseContext> logger,
			Version targetVersion,
			DatabaseType currentDatabaseType,
			CancellationToken cancellationToken)
		{
			if(logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (targetVersion == null)
				throw new ArgumentNullException(nameof(targetVersion));
			if (targetVersion < new Version(4, 0))
				throw new ArgumentOutOfRangeException(nameof(targetVersion), targetVersion, "Not a valid V4 version!");

			if (currentDatabaseType == DatabaseType.PostgresSql && targetVersion < new Version(4, 3, 0))
				throw new NotSupportedException("Cannot migrate below version 4.3.0 with PostgresSql!");

			if (targetVersion < new Version(4, 1, 0))
				throw new NotSupportedException("Cannot migrate below version 4.1.0!");

			// Update this with new migrations as they are made
			string targetMigration = null;
			if (targetVersion < new Version(4, 7, 0))
				switch (currentDatabaseType)
				{
					case DatabaseType.MariaDB:
					case DatabaseType.MySql:
						targetMigration = nameof(MYAddAdditionalDDParameters);
						break;
					case DatabaseType.PostgresSql:
						targetMigration = nameof(PGAddAdditionalDDParameters);
						break;
					case DatabaseType.SqlServer:
						targetMigration = nameof(MSAddAdditionalDDParameters);
						break;
					case DatabaseType.Sqlite:
						targetMigration = nameof(SLAddAdditionalDDParameters);
						break;
					default:
						throw new ArgumentException($"Invalid DatabaseType: {currentDatabaseType}", nameof(currentDatabaseType));
				}

			if (targetVersion < new Version(4, 6, 0))
				switch (currentDatabaseType)
				{
					case DatabaseType.MariaDB:
					case DatabaseType.MySql:
						targetMigration = nameof(MYAddDeploymentColumns);
						break;
					case DatabaseType.PostgresSql:
						targetMigration = nameof(PGAddDeploymentColumns);
						break;
					case DatabaseType.SqlServer:
						targetMigration = nameof(MSAddDeploymentColumns);
						break;
					case DatabaseType.Sqlite:
						targetMigration = nameof(SLAddDeploymentColumns);
						break;
					default:
						throw new ArgumentException($"Invalid DatabaseType: {currentDatabaseType}", nameof(currentDatabaseType));
				}

			if (targetVersion < new Version(4, 5, 0))
				switch (currentDatabaseType)
				{
					case DatabaseType.MariaDB:
					case DatabaseType.MySql:
						targetMigration = nameof(MYAllowNullDMApi);
						break;
					case DatabaseType.PostgresSql:
						targetMigration = nameof(PGAllowNullDMApi);
						break;
					case DatabaseType.SqlServer:
						targetMigration = nameof(MSAllowNullDMApi);
						break;
					case DatabaseType.Sqlite:
						targetMigration = nameof(SLAllowNullDMApi);
						break;
					default:
						throw new ArgumentException($"Invalid DatabaseType: {currentDatabaseType}", nameof(currentDatabaseType));
				}

			if (targetVersion < new Version(4, 4, 0))
				switch (currentDatabaseType)
				{
					case DatabaseType.MariaDB:
					case DatabaseType.MySql:
						targetMigration = nameof(MYFixForeignKey);
						break;
					case DatabaseType.PostgresSql:
						targetMigration = nameof(PGCreate);
						break;
					case DatabaseType.SqlServer:
						targetMigration = nameof(MSRemoveSoftColumns);
						break;
					case DatabaseType.Sqlite:
						targetMigration = nameof(SLRemoveSoftColumns);
						break;
					default:
						throw new ArgumentException($"Invalid DatabaseType: {currentDatabaseType}", nameof(currentDatabaseType));
				}

			if (targetVersion < new Version(4, 2, 0))
				targetMigration = currentDatabaseType == DatabaseType.Sqlite ? nameof(SLRebuild) : nameof(MSFixCascadingDelete);

			if (targetMigration == null)
			{
				logger.LogDebug("No down migration required.");
				return;
			}

			string migrationSubstitution;
			switch (currentDatabaseType)
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
					throw new InvalidOperationException($"Invalid DatabaseType: {currentDatabaseType}");
			}

			if (migrationSubstitution != null)
				targetMigration = String.Format(CultureInfo.InvariantCulture, migrationSubstitution, targetMigration.Substring(2));

			// even though it clearly implements it in the DatabaseFacade definition this won't work without casting (╯ಠ益ಠ)╯︵ ┻━┻
			var dbServiceProvider = ((IInfrastructure<IServiceProvider>)Database).Instance;
			var migrator = dbServiceProvider.GetRequiredService<IMigrator>();

			logger.LogInformation("Migrating down to version {0}. Target: {1}", targetVersion, targetMigration);
			try
			{
				await migrator.MigrateAsync(targetMigration, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				logger.LogCritical(e, "Failed to migrate!");
			}
		}
	}
}
