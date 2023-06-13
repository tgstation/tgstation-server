using System;

using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1506

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Rebuild of the schema for SQLite.
	/// </summary>
	public partial class SLRebuild : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.CreateTable(
				name: "Instances",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Name = table.Column<string>(maxLength: 10000, nullable: false),
					Path = table.Column<string>(nullable: false),
					Online = table.Column<bool>(nullable: false),
					ConfigurationType = table.Column<int>(nullable: false),
					AutoUpdateInterval = table.Column<uint>(nullable: false),
					ChatBotLimit = table.Column<ushort>(nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Instances", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Users",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Enabled = table.Column<bool>(nullable: false),
					CreatedAt = table.Column<DateTimeOffset>(nullable: false),
					SystemIdentifier = table.Column<string>(nullable: true),
					Name = table.Column<string>(maxLength: 10000, nullable: false),
					AdministrationRights = table.Column<ulong>(nullable: false),
					InstanceManagerRights = table.Column<ulong>(nullable: false),
					PasswordHash = table.Column<string>(nullable: true),
					CreatedById = table.Column<long>(nullable: true),
					CanonicalName = table.Column<string>(nullable: false),
					LastPasswordUpdate = table.Column<DateTimeOffset>(nullable: true),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Users", x => x.Id);
					table.ForeignKey(
						name: "FK_Users_Users_CreatedById",
						column: x => x.CreatedById,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
				});

			migrationBuilder.CreateTable(
				name: "ChatBots",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Name = table.Column<string>(maxLength: 100, nullable: false),
					Enabled = table.Column<bool>(nullable: true),
					ReconnectionInterval = table.Column<uint>(nullable: false),
					ChannelLimit = table.Column<ushort>(nullable: false),
					Provider = table.Column<int>(nullable: false),
					ConnectionString = table.Column<string>(maxLength: 10000, nullable: false),
					InstanceId = table.Column<long>(nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_ChatBots", x => x.Id);
					table.ForeignKey(
						name: "FK_ChatBots_Instances_InstanceId",
						column: x => x.InstanceId,
						principalTable: "Instances",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "DreamDaemonSettings",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					AllowWebClient = table.Column<bool>(nullable: false),
					SecurityLevel = table.Column<int>(nullable: false),
					PrimaryPort = table.Column<ushort>(nullable: false),
					SecondaryPort = table.Column<ushort>(nullable: false),
					StartupTimeout = table.Column<uint>(nullable: false),
					AutoStart = table.Column<bool>(nullable: false),
					SoftRestart = table.Column<bool>(nullable: false),
					SoftShutdown = table.Column<bool>(nullable: false),
					ProcessId = table.Column<int>(nullable: true),
					AccessToken = table.Column<string>(nullable: true),
					InstanceId = table.Column<long>(nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_DreamDaemonSettings", x => x.Id);
					table.ForeignKey(
						name: "FK_DreamDaemonSettings_Instances_InstanceId",
						column: x => x.InstanceId,
						principalTable: "Instances",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "DreamMakerSettings",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					ProjectName = table.Column<string>(maxLength: 10000, nullable: true),
					ApiValidationPort = table.Column<ushort>(nullable: false),
					ApiValidationSecurityLevel = table.Column<int>(nullable: false),
					InstanceId = table.Column<long>(nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_DreamMakerSettings", x => x.Id);
					table.ForeignKey(
						name: "FK_DreamMakerSettings_Instances_InstanceId",
						column: x => x.InstanceId,
						principalTable: "Instances",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "RepositorySettings",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					CommitterName = table.Column<string>(maxLength: 10000, nullable: false),
					CommitterEmail = table.Column<string>(maxLength: 10000, nullable: false),
					AccessUser = table.Column<string>(maxLength: 10000, nullable: true),
					AccessToken = table.Column<string>(maxLength: 10000, nullable: true),
					PushTestMergeCommits = table.Column<bool>(nullable: false),
					ShowTestMergeCommitters = table.Column<bool>(nullable: false),
					AutoUpdatesKeepTestMerges = table.Column<bool>(nullable: false),
					AutoUpdatesSynchronize = table.Column<bool>(nullable: false),
					PostTestMergeComment = table.Column<bool>(nullable: false),
					InstanceId = table.Column<long>(nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_RepositorySettings", x => x.Id);
					table.ForeignKey(
						name: "FK_RepositorySettings_Instances_InstanceId",
						column: x => x.InstanceId,
						principalTable: "Instances",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "RevisionInformations",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					CommitSha = table.Column<string>(maxLength: 40, nullable: false),
					OriginCommitSha = table.Column<string>(maxLength: 40, nullable: false),
					InstanceId = table.Column<long>(nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_RevisionInformations", x => x.Id);
					table.ForeignKey(
						name: "FK_RevisionInformations_Instances_InstanceId",
						column: x => x.InstanceId,
						principalTable: "Instances",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "InstanceUsers",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					UserId = table.Column<long>(nullable: false),
					InstanceUserRights = table.Column<ulong>(nullable: false),
					ByondRights = table.Column<ulong>(nullable: false),
					DreamDaemonRights = table.Column<ulong>(nullable: false),
					DreamMakerRights = table.Column<ulong>(nullable: false),
					RepositoryRights = table.Column<ulong>(nullable: false),
					ChatBotRights = table.Column<ulong>(nullable: false),
					ConfigurationRights = table.Column<ulong>(nullable: false),
					InstanceId = table.Column<long>(nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_InstanceUsers", x => x.Id);
					table.ForeignKey(
						name: "FK_InstanceUsers_Instances_InstanceId",
						column: x => x.InstanceId,
						principalTable: "Instances",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_InstanceUsers_Users_UserId",
						column: x => x.UserId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "Jobs",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Description = table.Column<string>(nullable: false),
					ErrorCode = table.Column<uint>(nullable: true),
					ExceptionDetails = table.Column<string>(nullable: true),
					StartedAt = table.Column<DateTimeOffset>(nullable: false),
					StoppedAt = table.Column<DateTimeOffset>(nullable: true),
					Cancelled = table.Column<bool>(nullable: false),
					CancelRightsType = table.Column<ulong>(nullable: true),
					CancelRight = table.Column<ulong>(nullable: true),
					StartedById = table.Column<long>(nullable: false),
					CancelledById = table.Column<long>(nullable: true),
					InstanceId = table.Column<long>(nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Jobs", x => x.Id);
					table.ForeignKey(
						name: "FK_Jobs_Users_CancelledById",
						column: x => x.CancelledById,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
					table.ForeignKey(
						name: "FK_Jobs_Instances_InstanceId",
						column: x => x.InstanceId,
						principalTable: "Instances",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_Jobs_Users_StartedById",
						column: x => x.StartedById,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "ChatChannels",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					IrcChannel = table.Column<string>(maxLength: 100, nullable: true),
					DiscordChannelId = table.Column<ulong>(nullable: true),
					IsAdminChannel = table.Column<bool>(nullable: false),
					IsWatchdogChannel = table.Column<bool>(nullable: false),
					IsUpdatesChannel = table.Column<bool>(nullable: false),
					Tag = table.Column<string>(maxLength: 10000, nullable: true),
					ChatSettingsId = table.Column<long>(nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_ChatChannels", x => x.Id);
					table.ForeignKey(
						name: "FK_ChatChannels_ChatBots_ChatSettingsId",
						column: x => x.ChatSettingsId,
						principalTable: "ChatBots",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "TestMerges",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Number = table.Column<int>(nullable: false),
					PullRequestRevision = table.Column<string>(maxLength: 40, nullable: false),
					Comment = table.Column<string>(maxLength: 10000, nullable: true),
					TitleAtMerge = table.Column<string>(nullable: false),
					BodyAtMerge = table.Column<string>(nullable: false),
					Url = table.Column<string>(nullable: false),
					Author = table.Column<string>(nullable: false),
					MergedAt = table.Column<DateTimeOffset>(nullable: false),
					MergedById = table.Column<long>(nullable: false),
					PrimaryRevisionInformationId = table.Column<long>(nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_TestMerges", x => x.Id);
					table.ForeignKey(
						name: "FK_TestMerges_Users_MergedById",
						column: x => x.MergedById,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
					table.ForeignKey(
						name: "FK_TestMerges_RevisionInformations_PrimaryRevisionInformationId",
						column: x => x.PrimaryRevisionInformationId,
						principalTable: "RevisionInformations",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "CompileJobs",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					DmeName = table.Column<string>(nullable: false),
					Output = table.Column<string>(nullable: false),
					DirectoryName = table.Column<Guid>(nullable: false),
					MinimumSecurityLevel = table.Column<int>(nullable: false),
					JobId = table.Column<long>(nullable: false),
					RevisionInformationId = table.Column<long>(nullable: false),
					ByondVersion = table.Column<string>(nullable: false),
					DMApiMajorVersion = table.Column<int>(nullable: true),
					DMApiMinorVersion = table.Column<int>(nullable: true),
					DMApiPatchVersion = table.Column<int>(nullable: true),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_CompileJobs", x => x.Id);
					table.ForeignKey(
						name: "FK_CompileJobs_Jobs_JobId",
						column: x => x.JobId,
						principalTable: "Jobs",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_CompileJobs_RevisionInformations_RevisionInformationId",
						column: x => x.RevisionInformationId,
						principalTable: "RevisionInformations",
						principalColumn: "Id");
				});

			migrationBuilder.CreateTable(
				name: "RevInfoTestMerges",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					TestMergeId = table.Column<long>(nullable: false),
					RevisionInformationId = table.Column<long>(nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_RevInfoTestMerges", x => x.Id);
					table.ForeignKey(
						name: "FK_RevInfoTestMerges_RevisionInformations_RevisionInformationId",
						column: x => x.RevisionInformationId,
						principalTable: "RevisionInformations",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_RevInfoTestMerges_TestMerges_TestMergeId",
						column: x => x.TestMergeId,
						principalTable: "TestMerges",
						principalColumn: "Id");
				});

			migrationBuilder.CreateTable(
				name: "ReattachInformations",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					AccessIdentifier = table.Column<string>(nullable: false),
					ProcessId = table.Column<int>(nullable: false),
					IsPrimary = table.Column<bool>(nullable: false),
					Port = table.Column<ushort>(nullable: false),
					RebootState = table.Column<int>(nullable: false),
					LaunchSecurityLevel = table.Column<int>(nullable: false),
					CompileJobId = table.Column<long>(nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_ReattachInformations", x => x.Id);
					table.ForeignKey(
						name: "FK_ReattachInformations_CompileJobs_CompileJobId",
						column: x => x.CompileJobId,
						principalTable: "CompileJobs",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "WatchdogReattachInformations",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					AlphaIsActive = table.Column<bool>(nullable: false),
					InstanceId = table.Column<long>(nullable: false),
					AlphaId = table.Column<long>(nullable: true),
					BravoId = table.Column<long>(nullable: true),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_WatchdogReattachInformations", x => x.Id);
					table.ForeignKey(
						name: "FK_WatchdogReattachInformations_ReattachInformations_AlphaId",
						column: x => x.AlphaId,
						principalTable: "ReattachInformations",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
					table.ForeignKey(
						name: "FK_WatchdogReattachInformations_ReattachInformations_BravoId",
						column: x => x.BravoId,
						principalTable: "ReattachInformations",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
					table.ForeignKey(
						name: "FK_WatchdogReattachInformations_Instances_InstanceId",
						column: x => x.InstanceId,
						principalTable: "Instances",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_ChatBots_InstanceId_Name",
				table: "ChatBots",
				columns: new[] { "InstanceId", "Name" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_ChatChannels_ChatSettingsId_DiscordChannelId",
				table: "ChatChannels",
				columns: new[] { "ChatSettingsId", "DiscordChannelId" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_ChatChannels_ChatSettingsId_IrcChannel",
				table: "ChatChannels",
				columns: new[] { "ChatSettingsId", "IrcChannel" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_CompileJobs_DirectoryName",
				table: "CompileJobs",
				column: "DirectoryName");

			migrationBuilder.CreateIndex(
				name: "IX_CompileJobs_JobId",
				table: "CompileJobs",
				column: "JobId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_CompileJobs_RevisionInformationId",
				table: "CompileJobs",
				column: "RevisionInformationId");

			migrationBuilder.CreateIndex(
				name: "IX_DreamDaemonSettings_InstanceId",
				table: "DreamDaemonSettings",
				column: "InstanceId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_DreamMakerSettings_InstanceId",
				table: "DreamMakerSettings",
				column: "InstanceId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_Instances_Path",
				table: "Instances",
				column: "Path",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_InstanceUsers_InstanceId",
				table: "InstanceUsers",
				column: "InstanceId");

			migrationBuilder.CreateIndex(
				name: "IX_InstanceUsers_UserId_InstanceId",
				table: "InstanceUsers",
				columns: new[] { "UserId", "InstanceId" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_Jobs_CancelledById",
				table: "Jobs",
				column: "CancelledById");

			migrationBuilder.CreateIndex(
				name: "IX_Jobs_InstanceId",
				table: "Jobs",
				column: "InstanceId");

			migrationBuilder.CreateIndex(
				name: "IX_Jobs_StartedById",
				table: "Jobs",
				column: "StartedById");

			migrationBuilder.CreateIndex(
				name: "IX_ReattachInformations_CompileJobId",
				table: "ReattachInformations",
				column: "CompileJobId");

			migrationBuilder.CreateIndex(
				name: "IX_RepositorySettings_InstanceId",
				table: "RepositorySettings",
				column: "InstanceId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_RevInfoTestMerges_RevisionInformationId",
				table: "RevInfoTestMerges",
				column: "RevisionInformationId");

			migrationBuilder.CreateIndex(
				name: "IX_RevInfoTestMerges_TestMergeId",
				table: "RevInfoTestMerges",
				column: "TestMergeId");

			migrationBuilder.CreateIndex(
				name: "IX_RevisionInformations_InstanceId_CommitSha",
				table: "RevisionInformations",
				columns: new[] { "InstanceId", "CommitSha" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_TestMerges_MergedById",
				table: "TestMerges",
				column: "MergedById");

			migrationBuilder.CreateIndex(
				name: "IX_TestMerges_PrimaryRevisionInformationId",
				table: "TestMerges",
				column: "PrimaryRevisionInformationId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_Users_CanonicalName",
				table: "Users",
				column: "CanonicalName",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_Users_CreatedById",
				table: "Users",
				column: "CreatedById");

			migrationBuilder.CreateIndex(
				name: "IX_Users_SystemIdentifier",
				table: "Users",
				column: "SystemIdentifier",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_WatchdogReattachInformations_AlphaId",
				table: "WatchdogReattachInformations",
				column: "AlphaId");

			migrationBuilder.CreateIndex(
				name: "IX_WatchdogReattachInformations_BravoId",
				table: "WatchdogReattachInformations",
				column: "BravoId");

			migrationBuilder.CreateIndex(
				name: "IX_WatchdogReattachInformations_InstanceId",
				table: "WatchdogReattachInformations",
				column: "InstanceId",
				unique: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropTable(
				name: "ChatChannels");

			migrationBuilder.DropTable(
				name: "DreamDaemonSettings");

			migrationBuilder.DropTable(
				name: "DreamMakerSettings");

			migrationBuilder.DropTable(
				name: "InstanceUsers");

			migrationBuilder.DropTable(
				name: "RepositorySettings");

			migrationBuilder.DropTable(
				name: "RevInfoTestMerges");

			migrationBuilder.DropTable(
				name: "WatchdogReattachInformations");

			migrationBuilder.DropTable(
				name: "ChatBots");

			migrationBuilder.DropTable(
				name: "TestMerges");

			migrationBuilder.DropTable(
				name: "ReattachInformations");

			migrationBuilder.DropTable(
				name: "CompileJobs");

			migrationBuilder.DropTable(
				name: "Jobs");

			migrationBuilder.DropTable(
				name: "RevisionInformations");

			migrationBuilder.DropTable(
				name: "Users");

			migrationBuilder.DropTable(
				name: "Instances");
		}
	}
}
