using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
#if !DEBUG
using System.Linq;
#endif
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
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
		public DbSet<ChatSettings> ChatSettings { get; set; }

		/// <inheritdoc />
		public DbSet<DreamDaemonSettings> DreamDaemonSettings { get; set; }

		/// <inheritdoc />
		public DbSet<RepositorySettings> RepositorySettings { get; set; }

		/// <summary>
		/// The <see cref="InstanceUser"/>s in the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		public DbSet<InstanceUser> InstanceUsers { get; set; }

		/// <inheritdoc />
		public DbSet<ChatChannel> ChatChannels { get; set; }

		/// <inheritdoc />
		public DbSet<Job> Jobs { get; set; }

		/// <summary>
		/// The <see cref="TestMerge"/>s in the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		public DbSet<TestMerge> TestMerges { get; set; }

		/// <inheritdoc />
		public DbSet<ReattachInformation> ReattachInformations { get; set; }

		/// <inheritdoc />
		public DbSet<WatchdogReattachInformation> WatchdogReattachInformations { get; set; }

		/// <summary>
		/// The connection string for the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		protected string ConnectionString => databaseConfiguration.ConnectionString;

		/// <summary>
		/// The <see cref="DatabaseConfiguration"/> for the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		readonly DatabaseConfiguration databaseConfiguration;
		/// <summary>
		/// The <see cref="IDatabaseSeeder"/> for the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		readonly IDatabaseSeeder databaseSeeder;

		/// <summary>
		/// Construct a <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TParentContext}"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		/// <param name="databaseConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="databaseConfiguration"/></param>
		/// <param name="databaseSeeder">The value of <see cref="databaseSeeder"/></param>
		public DatabaseContext(DbContextOptions<TParentContext> dbContextOptions, IOptions<DatabaseConfiguration> databaseConfigurationOptions, IDatabaseSeeder databaseSeeder) : base(dbContextOptions)
		{
			databaseConfiguration = databaseConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(databaseConfigurationOptions));
			this.databaseSeeder = databaseSeeder ?? throw new ArgumentNullException(nameof(databaseSeeder));
		}

		/// <inheritdoc />
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			var userModel = modelBuilder.Entity<User>();
			userModel.HasIndex(x => x.CanonicalName).IsUnique();
			userModel.HasMany(x => x.TestMerges).WithOne(x => x.MergedBy).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<InstanceUser>().HasIndex(x => new { x.UserId, x.InstanceId }).IsUnique();

			var revInfo = modelBuilder.Entity<RevisionInformation>();
			revInfo.HasMany(x => x.CompileJobs).WithOne(x => x.RevisionInformation).OnDelete(DeleteBehavior.Cascade);
			revInfo.HasMany(x => x.TestMerges).WithOne(x => x.RevisionInformation).OnDelete(DeleteBehavior.Cascade);
			revInfo.HasIndex(x => x.CommitSha).IsUnique();

			var chatChannel = modelBuilder.Entity<ChatChannel>();
			chatChannel.HasIndex(x => new { x.ChatSettingsId, x.IrcChannel }).IsUnique();
			chatChannel.HasIndex(x => new { x.ChatSettingsId, x.DiscordChannelId }).IsUnique();
			chatChannel.HasOne(x => x.ChatSettings).WithMany(x => x.Channels).HasForeignKey(x => x.ChatSettingsId).OnDelete(DeleteBehavior.Cascade);

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
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			base.OnConfiguring(optionsBuilder);
		}

		/// <inheritdoc />
		public async Task Initialize(CancellationToken cancellationToken)
		{
#if DEBUG
			await Database.EnsureCreatedAsync().ConfigureAwait(false);
			var wasEmpty = (await Users.CountAsync().ConfigureAwait(false)) == 0;
#else
			var migrations = await Database.GetAppliedMigrationsAsync().ConfigureAwait(false);
			var wasEmpty = !migrations.Any();
			await Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
#endif
			if (wasEmpty)
				await databaseSeeder.SeedDatabase(this, cancellationToken).ConfigureAwait(false);
			else if(databaseConfiguration.ResetAdminPassword)
				await databaseSeeder.ResetAdminPassword(this, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task Save(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);
	}
}
