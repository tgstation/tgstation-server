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
		/// The <see cref="InstancePermissionSet"/>s in the <see cref="DatabaseContext"/>.
		/// </summary>
		public DbSet<InstancePermissionSet> InstancePermissionSets { get; set; }

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
		/// The <see cref="RevInfoTestMerge"/>s in the <see cref="DatabaseContext"/>
		/// </summary>
		public DbSet<RevInfoTestMerge> RevInfoTestMerges { get; set; }

		/// <summary>
		/// The <see cref="OAuthConnection"/>s in the <see cref="DatabaseContext"/>
		/// </summary>
		public DbSet<OAuthConnection> OAuthConnections { get; set; }

		/// <summary>
		/// The <see cref="PermissionSet"/>s in the <see cref="DatabaseContext"/>
		/// </summary>
		public DbSet<PermissionSet> PermissionSets { get; set; }

		/// <summary>
		/// The <see cref="UserGroup"/>s in the <see cref="DatabaseContext"/>
		/// </summary>
		public DbSet<UserGroup> Groups { get; set; }

		/// <summary>
		/// The <see cref="DeleteBehavior"/> for the <see cref="CompileJob"/>/<see cref="RevisionInformation"/> foreign key.
		/// </summary>
		protected virtual DeleteBehavior RevInfoCompileJobDeleteBehavior => DeleteBehavior.ClientNoAction;

		/// <inheritdoc />
		IDatabaseCollection<User> IDatabaseContext.Users => usersCollection;

		/// <inheritdoc />
		IDatabaseCollection<Instance> IDatabaseContext.Instances => instancesCollection;

		/// <inheritdoc />
		IDatabaseCollection<InstancePermissionSet> IDatabaseContext.InstancePermissionSets => instancePermissionSetsCollection;

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
		IDatabaseCollection<OAuthConnection> IDatabaseContext.OAuthConnections => oAuthConnections;

		/// <inheritdoc />
		IDatabaseCollection<UserGroup> IDatabaseContext.Groups => groups;

		/// <inheritdoc />
		IDatabaseCollection<PermissionSet> IDatabaseContext.PermissionSets => permissionSets;

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
		/// Backing field for <see cref="IDatabaseContext.InstancePermissionSets"/>.
		/// </summary>
		readonly IDatabaseCollection<InstancePermissionSet> instancePermissionSetsCollection;

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
		/// Backing field for <see cref="IDatabaseContext.OAuthConnections"/>.
		/// </summary>
		readonly IDatabaseCollection<OAuthConnection> oAuthConnections;

		/// <summary>
		/// Backing field for <see cref="IDatabaseContext.Groups"/>.
		/// </summary>
		readonly IDatabaseCollection<UserGroup> groups;

		/// <summary>
		/// Backing field for <see cref="IDatabaseContext.PermissionSets"/>.
		/// </summary>
		readonly IDatabaseCollection<PermissionSet> permissionSets;

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
		protected DatabaseContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
		{
			usersCollection = new DatabaseCollection<User>(Users);
			instancesCollection = new DatabaseCollection<Instance>(Instances);
			instancePermissionSetsCollection = new DatabaseCollection<InstancePermissionSet>(InstancePermissionSets);
			compileJobsCollection = new DatabaseCollection<CompileJob>(CompileJobs);
			repositorySettingsCollection = new DatabaseCollection<RepositorySettings>(RepositorySettings);
			dreamMakerSettingsCollection = new DatabaseCollection<DreamMakerSettings>(DreamMakerSettings);
			dreamDaemonSettingsCollection = new DatabaseCollection<DreamDaemonSettings>(DreamDaemonSettings);
			chatBotsCollection = new DatabaseCollection<ChatBot>(ChatBots);
			chatChannelsCollection = new DatabaseCollection<ChatChannel>(ChatChannels);
			revisionInformationsCollection = new DatabaseCollection<RevisionInformation>(RevisionInformations);
			jobsCollection = new DatabaseCollection<Job>(Jobs);
			reattachInformationsCollection = new DatabaseCollection<ReattachInformation>(ReattachInformations);
			oAuthConnections = new DatabaseCollection<OAuthConnection>(OAuthConnections);
			groups = new DatabaseCollection<UserGroup>(Groups);
			permissionSets = new DatabaseCollection<PermissionSet>(PermissionSets);
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
			userModel.HasMany(x => x.OAuthConnections).WithOne(x => x.User).OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<OAuthConnection>().HasIndex(x => new { x.Provider, x.ExternalUserId }).IsUnique();

			var groupsModel = modelBuilder.Entity<UserGroup>();
			groupsModel.HasIndex(x => x.Name).IsUnique();
			groupsModel.HasMany(x => x.Users).WithOne(x => x.Group).OnDelete(DeleteBehavior.ClientSetNull);

			var permissionSetModel = modelBuilder.Entity<PermissionSet>();
			permissionSetModel.HasOne(x => x.Group).WithOne(x => x.PermissionSet).OnDelete(DeleteBehavior.Cascade);
			permissionSetModel.HasOne(x => x.User).WithOne(x => x.PermissionSet).OnDelete(DeleteBehavior.Cascade);
			permissionSetModel.HasMany(x => x.InstancePermissionSets).WithOne(x => x.PermissionSet).OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<InstancePermissionSet>().HasIndex(x => new { x.PermissionSetId, x.InstanceId }).IsUnique();

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
			instanceModel.HasIndex(x => new { x.Path, x.SwarmIdentifer }).IsUnique();
			instanceModel.HasMany(x => x.ChatSettings).WithOne(x => x.Instance).OnDelete(DeleteBehavior.Cascade);
			instanceModel.HasOne(x => x.DreamDaemonSettings).WithOne(x => x.Instance).OnDelete(DeleteBehavior.Cascade);
			instanceModel.HasOne(x => x.DreamMakerSettings).WithOne(x => x.Instance).OnDelete(DeleteBehavior.Cascade);
			instanceModel.HasOne(x => x.RepositorySettings).WithOne(x => x.Instance).OnDelete(DeleteBehavior.Cascade);
			instanceModel.HasMany(x => x.RevisionInformations).WithOne(x => x.Instance).OnDelete(DeleteBehavior.Cascade);
			instanceModel.HasMany(x => x.InstancePermissionSets).WithOne(x => x.Instance).OnDelete(DeleteBehavior.Cascade);
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
		public static readonly Type MSLatestMigration = typeof(MSAddSwarmIdentifer);

		/// <summary>
		/// Used by unit tests to remind us to setup the correct MYSQL migration downgrades.
		/// </summary>
		public static readonly Type MYLatestMigration = typeof(MYAddSwarmIdentifer);

		/// <summary>
		/// Used by unit tests to remind us to setup the correct PostgresSQL migration downgrades.
		/// </summary>
		public static readonly Type PGLatestMigration = typeof(PGAddSwarmIdentifer);

		/// <summary>
		/// Used by unit tests to remind us to setup the correct SQLite migration downgrades.
		/// </summary>
		public static readonly Type SLLatestMigration = typeof(SLAddSwarmIdentifer);
#endif

		/// <inheritdoc />
#pragma warning disable CA1502 // Cyclomatic complexity
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

			if (currentDatabaseType == DatabaseType.MariaDB)
				currentDatabaseType = DatabaseType.MySql; // Keeping switch expressions while avoiding `or` syntax from C#9

			if (targetVersion < new Version(4, 1, 0))
				throw new NotSupportedException("Cannot migrate below version 4.1.0!");

			// Update this with new migrations as they are made
			string targetMigration = null;
			if (targetVersion < new Version(4, 7, 0))
				targetMigration = currentDatabaseType switch
				{
					DatabaseType.MySql => nameof(MYAddAdditionalDDParameters),
					DatabaseType.PostgresSql => nameof(PGAddAdditionalDDParameters),
					DatabaseType.SqlServer => nameof(MSAddAdditionalDDParameters),
					DatabaseType.Sqlite => nameof(SLAddAdditionalDDParameters),
					_ => throw new ArgumentException($"Invalid DatabaseType: {currentDatabaseType}", nameof(currentDatabaseType)),
				};
			if (targetVersion < new Version(4, 6, 0))
				targetMigration = currentDatabaseType switch
				{
					DatabaseType.MySql => nameof(MYAddDeploymentColumns),
					DatabaseType.PostgresSql => nameof(PGAddDeploymentColumns),
					DatabaseType.SqlServer => nameof(MSAddDeploymentColumns),
					DatabaseType.Sqlite => nameof(SLAddDeploymentColumns),
					_ => throw new ArgumentException($"Invalid DatabaseType: {currentDatabaseType}", nameof(currentDatabaseType)),
				};
			if (targetVersion < new Version(4, 5, 0))
				targetMigration = currentDatabaseType switch
				{
					DatabaseType.MySql => nameof(MYAllowNullDMApi),
					DatabaseType.PostgresSql => nameof(PGAllowNullDMApi),
					DatabaseType.SqlServer => nameof(MSAllowNullDMApi),
					DatabaseType.Sqlite => nameof(SLAllowNullDMApi),
					_ => throw new ArgumentException($"Invalid DatabaseType: {currentDatabaseType}", nameof(currentDatabaseType)),
				};
			if (targetVersion < new Version(4, 4, 0))
				targetMigration = currentDatabaseType switch
				{
					DatabaseType.MySql => nameof(MYFixForeignKey),
					DatabaseType.PostgresSql => nameof(PGCreate),
					DatabaseType.SqlServer => nameof(MSRemoveSoftColumns),
					DatabaseType.Sqlite => nameof(SLRemoveSoftColumns),
					_ => throw new ArgumentException($"Invalid DatabaseType: {currentDatabaseType}", nameof(currentDatabaseType)),
				};
			if (targetVersion < new Version(4, 2, 0))
				targetMigration = currentDatabaseType == DatabaseType.Sqlite ? nameof(SLRebuild) : nameof(MSFixCascadingDelete);

			if (targetMigration == null)
			{
				logger.LogDebug("No down migration required.");
				return;
			}

			// already setup
			var migrationSubstitution = currentDatabaseType switch
			{
				DatabaseType.SqlServer => null,// already setup
				DatabaseType.MySql => "MY{0}",
				DatabaseType.Sqlite => "SL{0}",
				DatabaseType.PostgresSql => "PG{0}",
				_ => throw new InvalidOperationException($"Invalid DatabaseType: {currentDatabaseType}"),
			};

			if (migrationSubstitution != null)
				targetMigration = String.Format(CultureInfo.InvariantCulture, migrationSubstitution, targetMigration[2..]);

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
#pragma warning restore CA1502 // Cyclomatic complexity
	}
}
