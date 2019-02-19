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
using Tgstation.Server.Host.Models.Migrations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	#pragma warning disable CA1506 // TODO: Decomplexify
	abstract class DatabaseContext<TParentContext> : DbContext, IDatabaseContext where TParentContext : DbContext
	{
		/// <inheritdoc />
		public DbSet<User> Users { get; set; }

		/// <inheritdoc />
		public DbSet<Instance> Instances { get; set; }

		/// <inheritdoc />
		public DbSet<CompileJob> CompileJobs { get; set; }

		/// <inheritdoc />
		public DbSet<RevisionInformation> RevisionInformations { get; set; }

		/// <inheritdoc />
		public DbSet<DreamMakerSettings> DreamMakerSettings { get; set; }

		/// <inheritdoc />
		public DbSet<ChatBot> ChatBots { get; set; }

		/// <inheritdoc />
		public DbSet<DreamDaemonSettings> DreamDaemonSettings { get; set; }

		/// <inheritdoc />
		public DbSet<RepositorySettings> RepositorySettings { get; set; }

		/// <inheritdoc />
		public DbSet<InstanceUser> InstanceUsers { get; set; }

		/// <inheritdoc />
		public DbSet<ChatChannel> ChatChannels { get; set; }

		/// <inheritdoc />
		public DbSet<Job> Jobs { get; set; }

		/// <inheritdoc />
		public DbSet<ReattachInformation> ReattachInformations { get; set; }

		/// <inheritdoc />
		public DbSet<WatchdogReattachInformation> WatchdogReattachInformations { get; set; }

		/// <summary>
		/// The <see cref="TestMerge"/>s in the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		public DbSet<TestMerge> TestMerges { get; set; }

		/// <summary>
		/// The <see cref="RevInfoTestMerge"/>s om the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		public DbSet<RevInfoTestMerge> RevInfoTestMerges { get; set; }

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		protected ILogger Logger { get; }

		/// <summary>
		/// The <see cref="DatabaseConfiguration"/> for the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		protected DatabaseConfiguration DatabaseConfiguration { get; }

		/// <summary>
		/// The <see cref="IDatabaseSeeder"/> for the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		readonly IDatabaseSeeder databaseSeeder;

		/// <summary>
		/// Construct a <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TParentContext}"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		/// <param name="databaseConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="DatabaseConfiguration"/></param>
		/// <param name="databaseSeeder">The value of <see cref="databaseSeeder"/></param>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		public DatabaseContext(DbContextOptions<TParentContext> dbContextOptions, IOptions<DatabaseConfiguration> databaseConfigurationOptions, IDatabaseSeeder databaseSeeder, ILogger logger) : base(dbContextOptions)
		{
			DatabaseConfiguration = databaseConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(databaseConfigurationOptions));
			this.databaseSeeder = databaseSeeder ?? throw new ArgumentNullException(nameof(databaseSeeder));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			Logger.LogTrace("Building entity framework context...");
			base.OnModelCreating(modelBuilder);

			var userModel = modelBuilder.Entity<User>();
			userModel.HasIndex(x => x.CanonicalName).IsUnique();
			userModel.HasMany(x => x.TestMerges).WithOne(x => x.MergedBy).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<InstanceUser>().HasIndex(x => new { x.UserId, x.InstanceId }).IsUnique();

			modelBuilder.Entity<TestMerge>().HasMany(x => x.RevisonInformations).WithOne(x => x.TestMerge).OnDelete(DeleteBehavior.Cascade);

			var revInfo = modelBuilder.Entity<RevisionInformation>();
			revInfo.HasMany(x => x.CompileJobs).WithOne(x => x.RevisionInformation).OnDelete(DeleteBehavior.Cascade);
			revInfo.HasMany(x => x.ActiveTestMerges).WithOne(x => x.RevisionInformation).OnDelete(DeleteBehavior.Cascade);
			revInfo.HasOne(x => x.PrimaryTestMerge).WithOne(x => x.PrimaryRevisionInformation).OnDelete(DeleteBehavior.Restrict);
			revInfo.HasIndex(x => new { x.InstanceId, x.CommitSha }).IsUnique();

			modelBuilder.Entity<CompileJob>().HasIndex(x => x.DirectoryName);

			modelBuilder.Entity<Job>().HasOne<CompileJob>().WithOne(x => x.Job).OnDelete(DeleteBehavior.Restrict);

			var chatChannel = modelBuilder.Entity<ChatChannel>();
			chatChannel.HasIndex(x => new { x.ChatSettingsId, x.IrcChannel }).IsUnique();
			chatChannel.HasIndex(x => new { x.ChatSettingsId, x.DiscordChannelId }).IsUnique();
			chatChannel.HasOne(x => x.ChatSettings).WithMany(x => x.Channels).HasForeignKey(x => x.ChatSettingsId).OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<ChatBot>().HasIndex(x => x.Name).IsUnique();

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

			wasEmpty |= (await Users.CountAsync(cancellationToken).ConfigureAwait(false)) == 0;

			if (wasEmpty)
			{
				Logger.LogInformation("Seeding database...");
				await databaseSeeder.SeedDatabase(this, cancellationToken).ConfigureAwait(false);
			}
			else if (DatabaseConfiguration.ResetAdminPassword)
			{
				Logger.LogWarning("Enabling and resetting admin password due to configuration!");
				await databaseSeeder.ResetAdminPassword(this, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public Task Save(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);

		/// <summary>
		/// If the MY_ class of migrations should be used instead of the MS_ class
		/// </summary>
		/// <returns><see langword="true"/> if the MY_ class of migrations should be used instead of the MS_ class, <see langword="false"/> otherwise</returns>
		protected abstract bool UseMySQLMigrations();

		/// <inheritdoc />
		public async Task SchemaDowngradeForServerVersion(Version version, CancellationToken cancellationToken)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));
			if (version < new Version(4, 0))
				throw new ArgumentOutOfRangeException(nameof(version), version, "Not a valid V4 version!");

			string targetMigration = null;

			// Update this with new migrations as they are made
			// Always use the MS class
			if (version < new Version(4, 0, 2))
				targetMigration = nameof(MSReattachCompileJobRequired);

			if (targetMigration == null)
				return;

			if (UseMySQLMigrations())
				targetMigration = String.Format(CultureInfo.InvariantCulture, "MY{0}", targetMigration.Substring(2));

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
	}
}
