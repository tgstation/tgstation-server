using System;

using Microsoft.EntityFrameworkCore;

using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// <see cref="DatabaseContext"/> for MySQL.
	/// </summary>
	sealed class MySqlDatabaseContext : DatabaseContext
	{
		/// <inheritdoc />
		protected override DeleteBehavior RevInfoCompileJobDeleteBehavior => DeleteBehavior.Cascade;

		/// <summary>
		/// Initializes a new instance of the <see cref="MySqlDatabaseContext"/> class.
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext"/>.</param>
		public MySqlDatabaseContext(DbContextOptions<MySqlDatabaseContext> dbContextOptions)
			: base(dbContextOptions)
		{
		}

		/// <summary>
		/// Configure the <see cref="MySqlDatabaseContext"/>.
		/// </summary>
		/// <param name="options">The <see cref="DbContextOptionsBuilder"/> to configure.</param>
		/// <param name="databaseConfiguration">The <see cref="DatabaseConfiguration"/>.</param>
		public static void ConfigureWith(DbContextOptionsBuilder options, DatabaseConfiguration databaseConfiguration)
		{
			ArgumentNullException.ThrowIfNull(options);
			ArgumentNullException.ThrowIfNull(databaseConfiguration);

			if (databaseConfiguration.DatabaseType != DatabaseType.MariaDB && databaseConfiguration.DatabaseType != DatabaseType.MySql)
				throw new InvalidOperationException($"Invalid DatabaseType for {nameof(MySqlDatabaseContext)}!");

			ServerVersion serverVersion;
			if (!String.IsNullOrEmpty(databaseConfiguration.ServerVersion))
			{
				serverVersion = ServerVersion.Parse(
					databaseConfiguration.ServerVersion,
					databaseConfiguration.DatabaseType == DatabaseType.MariaDB
						? ServerType.MariaDb
						: ServerType.MySql);
			}
			else
				serverVersion = ServerVersion.AutoDetect(databaseConfiguration.ConnectionString);

			options.UseMySql(
				databaseConfiguration.ConnectionString,
				serverVersion,
				mySqlOptions =>
				{
					mySqlOptions.TranslateParameterizedCollectionsToConstants();
					mySqlOptions.EnableRetryOnFailure();
					mySqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
				});
		}

		/// <inheritdoc />
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Added to prevent a column type change after upgrading Pomelo.Mysql
			// Related: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1606
			modelBuilder
				.MapMySqlTextField<ChatBot>(x => x.ConnectionString)
				.MapMySqlTextField<ChatChannel>(x => x.Tag)
				.MapMySqlTextField<ChatChannel>(x => x.IrcChannel)
				.MapMySqlTextField<CompileJob>(x => x.EngineVersion)
				.MapMySqlTextField<CompileJob>(x => x.DmeName)
				.MapMySqlTextField<CompileJob>(x => x.Output)
				.MapMySqlTextField<CompileJob>(x => x.RepositoryOrigin)
				.MapMySqlTextField<DreamDaemonSettings>(x => x.AdditionalParameters)
				.MapMySqlTextField<DreamMakerSettings>(x => x.ProjectName)
				.MapMySqlTextField<Instance>(x => x.Name)
				.MapMySqlTextField<Instance>(x => x.Path)
				.MapMySqlTextField<Instance>(x => x.SwarmIdentifer)
				.MapMySqlTextField<Job>(x => x.Description)
				.MapMySqlTextField<Job>(x => x.ExceptionDetails)
				.MapMySqlTextField<OAuthConnection>(x => x.ExternalUserId)
				.MapMySqlTextField<ReattachInformation>(x => x.AccessIdentifier)
				.MapMySqlTextField<RepositorySettings>(x => x.AccessToken)
				.MapMySqlTextField<RepositorySettings>(x => x.AccessUser)
				.MapMySqlTextField<RepositorySettings>(x => x.CommitterEmail)
				.MapMySqlTextField<RepositorySettings>(x => x.CommitterName)
				.MapMySqlTextField<RevisionInformation>(x => x.CommitSha)
				.MapMySqlTextField<RevisionInformation>(x => x.OriginCommitSha)
				.MapMySqlTextField<TestMerge>(x => x.Author)
				.MapMySqlTextField<TestMerge>(x => x.BodyAtMerge)
				.MapMySqlTextField<TestMerge>(x => x.Comment)
				.MapMySqlTextField<TestMerge>(x => x.TargetCommitSha)
				.MapMySqlTextField<TestMerge>(x => x.SourceRepository)
				.MapMySqlTextField<TestMerge>(x => x.TitleAtMerge)
				.MapMySqlTextField<TestMerge>(x => x.Url)
				.MapMySqlTextField<User>(x => x.CanonicalName)
				.MapMySqlTextField<User>(x => x.Name)
				.MapMySqlTextField<User>(x => x.PasswordHash)
				.MapMySqlTextField<User>(x => x.SystemIdentifier)
				.MapMySqlTextField<UserGroup>(x => x.Name);
		}
	}
}
