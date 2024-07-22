using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database.Migrations;
using Tgstation.Server.Host.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tgstation.Server.Tests
{
	[TestClass]
	[TestCategory("RequiresDatabase")]
	public sealed class TestDatabase
	{
		[TestMethod]
		public async Task TestDownMigrations()
		{
			var connectionString = Environment.GetEnvironmentVariable("TGS_TEST_CONNECTION_STRING");

			if (string.IsNullOrEmpty(connectionString))
				Assert.Inconclusive("No connection string configured in env var TGS_TEST_CONNECTION_STRING!");

			var databaseTypeString = Environment.GetEnvironmentVariable("TGS_TEST_DATABASE_TYPE");
			if (!Enum.TryParse<DatabaseType>(databaseTypeString, out var databaseType))
				Assert.Inconclusive("No/invalid database type configured in env var TGS_TEST_DATABASE_TYPE!");

			string migrationName = null;
			DatabaseContext CreateContext()
			{
				string serverVersion = Environment.GetEnvironmentVariable($"{DatabaseConfiguration.Section}__{nameof(DatabaseConfiguration.ServerVersion)}");
				if (string.IsNullOrWhiteSpace(serverVersion))
					serverVersion = null;
				switch (databaseType)
				{
					case DatabaseType.MySql:
					case DatabaseType.MariaDB:
						migrationName = nameof(MYInitialCreate);
						return new MySqlDatabaseContext(
							Host.Database.Design.DesignTimeDbContextFactoryHelpers.CreateDatabaseContextOptions<MySqlDatabaseContext>(
								databaseType,
								connectionString,
								serverVersion));
					case DatabaseType.PostgresSql:
						migrationName = nameof(PGCreate);
						return new PostgresSqlDatabaseContext(
							Host.Database.Design.DesignTimeDbContextFactoryHelpers.CreateDatabaseContextOptions<PostgresSqlDatabaseContext>(
								databaseType,
								connectionString,
								serverVersion));
					case DatabaseType.SqlServer:
						migrationName = nameof(MSInitialCreate);
						return new SqlServerDatabaseContext(
							Host.Database.Design.DesignTimeDbContextFactoryHelpers.CreateDatabaseContextOptions<SqlServerDatabaseContext>(
								databaseType,
								connectionString,
								serverVersion));
					case DatabaseType.Sqlite:
						migrationName = nameof(SLRebuild);
						return new SqliteDatabaseContext(
							Host.Database.Design.DesignTimeDbContextFactoryHelpers.CreateDatabaseContextOptions<SqliteDatabaseContext>(
								databaseType,
								connectionString,
								serverVersion));
				}

				return null;
			}

			await using var context = CreateContext();
			await context.Database.EnsureDeletedAsync();
			await context.Database.MigrateAsync(default);

			// add usergroups and dummy instances for testing purposes
			var group = new Host.Models.UserGroup
			{
				PermissionSet = new Host.Models.PermissionSet
				{
					AdministrationRights = AdministrationRights.ChangeVersion,
					InstanceManagerRights = InstanceManagerRights.GrantPermissions
				},
				Name = "TestGroup",
			};

			const string TestUserName = "TestUser42";
			var user = new Host.Models.User
			{
				Name = TestUserName,
				CreatedAt = DateTimeOffset.UtcNow,
				OAuthConnections = new List<Host.Models.OAuthConnection>(),
				CanonicalName = Host.Models.User.CanonicalizeName(TestUserName),
				Enabled = false,
				Group = group,
				PasswordHash = "_",
			};

			var instance = new Host.Models.Instance
			{
				AutoUpdateInterval = 0,
				AutoUpdateCron = String.Empty,
				ChatBotLimit = 1,
				ChatSettings = new List<Host.Models.ChatBot>(),
				ConfigurationType = ConfigurationType.HostWrite,
				DreamDaemonSettings = new Host.Models.DreamDaemonSettings
				{
					AllowWebClient = false,
					AutoStart = false,
					HealthCheckSeconds = 0,
					DumpOnHealthCheckRestart = false,
					Port = 1447,
					OpenDreamTopicPort = 0,
					SecurityLevel = DreamDaemonSecurity.Safe,
					Visibility = DreamDaemonVisibility.Public,
					StartupTimeout = 1000,
					TopicRequestTimeout = 1000,
					AdditionalParameters = string.Empty,
					StartProfiler = false,
					LogOutput = true,
					MapThreads = 69,
					Minidumps = true,
				},
				DreamMakerSettings = new Host.Models.DreamMakerSettings
				{
					ApiValidationPort = 1557,
					ApiValidationSecurityLevel = DreamDaemonSecurity.Trusted,
					RequireDMApiValidation = false,
					Timeout = TimeSpan.FromSeconds(13),
				},
				InstancePermissionSets = new List<Host.Models.InstancePermissionSet>
				{
					new Host.Models.InstancePermissionSet
					{
						EngineRights = EngineRights.InstallCustomByondVersion,
						ChatBotRights = ChatBotRights.None,
						ConfigurationRights = ConfigurationRights.Read,
						DreamDaemonRights = DreamDaemonRights.ReadRevision,
						DreamMakerRights = DreamMakerRights.SetApiValidationPort,
						InstancePermissionSetRights = InstancePermissionSetRights.Write,
						PermissionSet = group.PermissionSet,
						RepositoryRights = RepositoryRights.SetReference
					}
				},
				Name = "sfdsadfsa",
				Online = false,
				Path = "/a/b/c/d",
				RepositorySettings = new Host.Models.RepositorySettings
				{
					AutoUpdatesKeepTestMerges = false,
					AutoUpdatesSynchronize = false,
					CommitterEmail = "email@eample.com",
					CommitterName = "blubluh",
					CreateGitHubDeployments = false,
					PostTestMergeComment = false,
					PushTestMergeCommits = false,
					ShowTestMergeCommitters = false,
					UpdateSubmodules = false,
				},
			};

			context.Users.Add(user);
			context.Groups.Add(group);
			context.Instances.Add(instance);
			await context.Save(default);

			var dbServiceProvider = ((IInfrastructure<IServiceProvider>)context.Database).Instance;
			var migrator = dbServiceProvider.GetRequiredService<IMigrator>();
			await migrator.MigrateAsync(migrationName, default);
			await context.Database.EnsureDeletedAsync();
		}
	}
}
