using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
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

		/// <summary>
		/// The <see cref="DbSet{TEntity}"/> for <see cref="Log"/>s
		/// </summary>
		public DbSet<Log> Logs { get; set; }

		/// <summary>
		/// The <see cref="InstanceUser"/>s in the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		public DbSet<InstanceUser> InstanceUsers { get; set; }
		/// <summary>
		/// The <see cref="ChatChannel"/>s in the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		public DbSet<ChatChannel> ChatChannels { get; set; }
		/// <summary>
		/// The <see cref="ChatSettings"/>s in the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		public DbSet<ChatSettings> ChatSettings { get; set; }
		/// <summary>
		/// The <see cref="Models.DreamDaemonSettings"/> in the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		public DbSet<DreamDaemonSettings> DreamDaemonSettings { get; set; }
		/// <summary>
		/// The <see cref="Models.DreamMakerSettings"/> in the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		public DbSet<DreamMakerSettings> DreamMakerSettings { get; set; }
		/// <summary>
		/// The <see cref="CompileJob"/>s in the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		public DbSet<CompileJob> CompileJobs { get; set; }
		/// <summary>
		/// The <see cref="Job"/>s in the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		public DbSet<Job> Jobs { get; set; }
		/// <summary>
		/// The <see cref="TestMerge"/>s in the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		public DbSet<TestMerge> TestMerges { get; set; }
		/// <summary>
		/// The <see cref="RevisionInformation"/>s in the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		public DbSet<RevisionInformation> RevisionInformations { get; set; }
		/// <summary>
		/// The <see cref="Models.RepositorySettings"/> in the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		public DbSet<RepositorySettings> RepositorySettings { get; set; }

		/// <summary>
		/// The connection string for the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		protected string ConnectionString => databaseConfiguration.ConnectionString;

		/// <summary>
		/// The <see cref="DatabaseConfiguration"/> for the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		readonly DatabaseConfiguration databaseConfiguration;
		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		readonly ILoggerFactory loggerFactory;
		/// <summary>
		/// The <see cref="IDatabaseSeeder"/> for the <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		readonly IDatabaseSeeder databaseSeeder;

		/// <summary>
		/// Construct a <see cref="DatabaseContext{TParentContext}"/>
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TParentContext}"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		/// <param name="databaseConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="databaseConfiguration"/></param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/></param>
		/// <param name="databaseSeeder">The value of <see cref="databaseSeeder"/></param>
		public DatabaseContext(DbContextOptions<TParentContext> dbContextOptions, IOptions<DatabaseConfiguration> databaseConfigurationOptions, ILoggerFactory loggerFactory, IDatabaseSeeder databaseSeeder) : base(dbContextOptions)
		{
			databaseConfiguration = databaseConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(databaseConfigurationOptions));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.databaseSeeder = databaseSeeder ?? throw new ArgumentNullException(nameof(databaseSeeder));
		}

		/// <inheritdoc />
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// build default model.
			LogModelBuilderHelper.Build(modelBuilder.Entity<Log>());
			modelBuilder.Entity<Log>().ToTable(nameof(Logs));

			modelBuilder.Entity<RevisionInformation>().HasIndex(x => x.Revision).IsUnique();
			modelBuilder.Entity<User>().HasIndex(x => new { x.Name, x.SystemIdentifier }).IsUnique();
		}

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			base.OnConfiguring(optionsBuilder);

			optionsBuilder.UseLoggerFactory(loggerFactory);
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
#else
			var migrations = await Database.GetAppliedMigrationsAsync().ConfigureAwait(false);
			var wasEmpty = !migrations.Any();
			await Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
#endif
			if (wasEmpty)
				await databaseSeeder.SeedDatabase(this, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task Save(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);
	}
}
