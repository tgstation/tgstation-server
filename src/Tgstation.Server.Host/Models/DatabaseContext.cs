using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
#if !DEBUG
using System.Linq;
#endif
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Configuration;
using ZNetCS.AspNetCore.Logging.EntityFrameworkCore;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	abstract class DatabaseContext<TParentContext> : DbContext, IDatabaseContext where TParentContext : DbContext
	{
		/// <inheritdoc />
		public DbSet<ServerSettings> ServerSettings { get; set; }

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
		/// The <see cref="DbSet{TEntity}"/> for <see cref="Log"/>s
		/// </summary>
		public DbSet<Log> Logs { get; set; }

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

		/// <summary>
		/// The <see cref="DbSet{TEntity}"/> for <see cref="Models.ReattachInformation"/>s
		/// </summary>
		public DbSet<ReattachInformation> ReattachInformation { get; set; }

		/// <summary>
		/// The <see cref="DbSet{TEntity}"/> for <see cref="Models.WatchdogReattachInformation"/>s
		/// </summary>
		public DbSet<WatchdogReattachInformation> WatchdogReattachInformation { get; set; }

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

			// build default model.
			LogModelBuilderHelper.Build(modelBuilder.Entity<Log>());
			modelBuilder.Entity<Log>().ToTable(nameof(Logs));

			modelBuilder.Entity<RevisionInformation>().HasIndex(x => x.Commit).IsUnique();
			var user = modelBuilder.Entity<User>();
			user.HasIndex(x => x.CanonicalName).IsUnique();
			user.HasOne(x => x.CreatedBy).WithMany(x => x.CreatedUsers).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<InstanceUser>().HasIndex(x => new { x.UserId, x.Instance }).IsUnique();

			var chatChannel = modelBuilder.Entity<ChatChannel>();
			chatChannel.HasIndex(x => new { x.ChatSettingsId, x.IrcChannel }).IsUnique();
			chatChannel.HasIndex(x => new { x.ChatSettingsId, x.DiscordChannelId }).IsUnique();
			chatChannel.HasOne(x => x.ChatSettings).WithMany(x => x.Channels).HasForeignKey(x => x.ChatSettingsId).OnDelete(DeleteBehavior.Cascade);
		}

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			base.OnConfiguring(optionsBuilder);
		}

		/// <inheritdoc />
		public async Task<ServerSettings> GetServerSettings(CancellationToken cancellationToken)
		{
			var settings = await ServerSettings.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (settings == default(ServerSettings))
			{
				settings = new ServerSettings();
				ServerSettings.Add(settings);
			}
			return settings;
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
