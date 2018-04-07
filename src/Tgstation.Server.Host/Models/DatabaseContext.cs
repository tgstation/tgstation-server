using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Configuration;
using ZNetCS.AspNetCore.Logging.EntityFrameworkCore;

namespace Tgstation.Server.Host.Models
{
	sealed class DatabaseContext : DbContext, IDatabaseContext
	{
		public DbSet<ServerSettings> ServerSettings { get; set; }

		public DbSet<User> Users { get; set; }

		public DbSet<Instance> Instances { get; set; }

		/// <summary>
		/// The <see cref="DbSet{TEntity}"/> for <see cref="Log"/>s
		/// </summary>
		public DbSet<Log> Logs { get; set; }

		/// <summary>
		/// The <see cref="DatabaseConfiguration"/> for the <see cref="DatabaseContext"/>
		/// </summary>
		readonly DatabaseConfiguration databaseConfiguration;
		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="DatabaseContext"/>
		/// </summary>
		readonly ILoggerFactory loggerFactory;
		/// <summary>
		/// The <see cref="IHostingEnvironment"/> for the <see cref="DatabaseContext"/>
		/// </summary>
		readonly IHostingEnvironment hostingEnvironment;

		/// <summary>
		/// Construct a <see cref="DatabaseContext"/>
		/// </summary>
		/// <param name="options">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext"/></param>
		/// <param name="databaseConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="databaseConfiguration"/></param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/></param>
		/// <param name="hostingEnvironment">The value of <see cref="hostingEnvironment"/></param>
		public DatabaseContext(DbContextOptions<DatabaseContext> options, IOptions<DatabaseConfiguration> databaseConfigurationOptions, ILoggerFactory loggerFactory, IHostingEnvironment hostingEnvironment) : base(options)
		{
			databaseConfiguration = databaseConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(databaseConfigurationOptions));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
		}

		/// <inheritdoc />
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// build default model.
			LogModelBuilderHelper.Build(modelBuilder.Entity<Log>());
			// real relation database can map table:
			modelBuilder.Entity<Log>().ToTable(nameof(Log));
		}

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			switch (databaseConfiguration.DatabaseType)
			{
				case DatabaseType.MySql:
					optionsBuilder.UseMySQL(databaseConfiguration.ConnectionString);
					break;
				case DatabaseType.Sqlite:
					optionsBuilder.UseSqlite(databaseConfiguration.ConnectionString);
					break;
				case DatabaseType.SqlServer:
					optionsBuilder.UseSqlServer(databaseConfiguration.ConnectionString);
					break;
			}
			optionsBuilder.UseLoggerFactory(loggerFactory);
			if (hostingEnvironment.IsDevelopment())
				optionsBuilder.EnableSensitiveDataLogging();
		}

		/// <inheritdoc />
		public Task<ServerSettings> GetServerSettings(CancellationToken cancellationToken) => ServerSettings.FirstOrDefaultAsync(cancellationToken);

		/// <inheritdoc />
		public async Task Initialize(CancellationToken cancellationToken)
		{
			await Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
			await Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task Save(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);
	}
}
