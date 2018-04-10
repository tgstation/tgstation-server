using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Models.Migrations.SqlServer
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Instances",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: false),
                    Path = table.Column<string>(nullable: false),
                    Online = table.Column<bool>(nullable: false),
                    ConfigurationAllowed = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    EventId = table.Column<int>(nullable: false),
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Level = table.Column<int>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    Name = table.Column<string>(maxLength: 255, nullable: true),
                    TimeStamp = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RevisionInformations",
                columns: table => new
                {
                    Revision = table.Column<string>(maxLength: 40, nullable: false),
                    OriginRevision = table.Column<string>(maxLength: 40, nullable: false),
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisionInformations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServerSettings",
                columns: table => new
                {
                    SystemAuthenticationGroup = table.Column<string>(nullable: true),
                    EnableTelemetry = table.Column<bool>(nullable: false),
                    UpstreamRepository = table.Column<string>(nullable: true),
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    AdministrationRights = table.Column<int>(nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    InstanceManagerRights = table.Column<int>(nullable: false),
                    SystemIdentifier = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: true),
                    TokenSecret = table.Column<string>(maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatSettings",
                columns: table => new
                {
                    IrcEnabled = table.Column<bool>(nullable: false),
                    IrcHost = table.Column<string>(nullable: false),
                    IrcPort = table.Column<int>(nullable: false),
                    IrcNickServPassword = table.Column<string>(nullable: true),
                    DiscordEnabled = table.Column<bool>(nullable: false),
                    DiscordBotToken = table.Column<string>(nullable: true),
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    InstanceId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatSettings_Instances_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepositorySettings",
                columns: table => new
                {
                    Origin = table.Column<string>(nullable: true),
                    CommitterName = table.Column<string>(nullable: false),
                    CommitterEmail = table.Column<string>(nullable: false),
                    AccessUser = table.Column<string>(nullable: true),
                    AccessToken = table.Column<string>(nullable: true),
                    PushTestMergeCommits = table.Column<bool>(nullable: false),
                    AutoUpdateInterval = table.Column<int>(nullable: true),
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    InstanceId = table.Column<long>(nullable: false),
                    RevisionInformationId = table.Column<long>(nullable: true)
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
                    table.ForeignKey(
                        name: "FK_RepositorySettings_RevisionInformations_RevisionInformationId",
                        column: x => x.RevisionInformationId,
                        principalTable: "RevisionInformations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompileJobs",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    StartedAt = table.Column<DateTimeOffset>(nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(nullable: false),
                    TargetedPrimaryDirectory = table.Column<bool>(nullable: true),
                    Output = table.Column<string>(nullable: true),
                    ExitCode = table.Column<int>(nullable: true),
                    TriggeredById = table.Column<long>(nullable: false),
                    RevisionInformationId = table.Column<long>(nullable: false),
                    InstanceId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompileJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompileJobs_Instances_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompileJobs_RevisionInformations_RevisionInformationId",
                        column: x => x.RevisionInformationId,
                        principalTable: "RevisionInformations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompileJobs_Users_TriggeredById",
                        column: x => x.TriggeredById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InstanceUsers",
                columns: table => new
                {
                    ByondRights = table.Column<int>(nullable: false),
                    DreamDaemonRights = table.Column<int>(nullable: false),
                    DreamMakerRights = table.Column<int>(nullable: false),
                    RepositoryRights = table.Column<int>(nullable: false),
                    ChatSettingsRights = table.Column<int>(nullable: false),
                    ConfigurationRights = table.Column<int>(nullable: false),
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    InstanceId = table.Column<long>(nullable: true),
                    UserId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstanceUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstanceUsers_Instances_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstanceUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Description = table.Column<string>(nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(nullable: false),
                    StoppedAt = table.Column<DateTimeOffset>(nullable: false),
                    Cancelled = table.Column<bool>(nullable: false),
                    StartedById = table.Column<long>(nullable: false),
                    InstanceId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
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
                name: "TestMerges",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    MergedAt = table.Column<DateTimeOffset>(nullable: false),
                    TitleAtMerge = table.Column<string>(nullable: false),
                    BodyAtMerge = table.Column<string>(nullable: false),
                    Author = table.Column<string>(nullable: false),
                    MergedById = table.Column<long>(nullable: false),
                    InstanceId = table.Column<long>(nullable: true),
                    RevisionInformationId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestMerges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestMerges_Instances_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestMerges_Users_MergedById",
                        column: x => x.MergedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestMerges_RevisionInformations_RevisionInformationId",
                        column: x => x.RevisionInformationId,
                        principalTable: "RevisionInformations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatChannels",
                columns: table => new
                {
                    IrcChannel = table.Column<string>(nullable: true),
                    DiscordChannelId = table.Column<long>(nullable: false),
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ChatSettingsId = table.Column<long>(nullable: true),
                    ChatSettingsId1 = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatChannels_ChatSettings_ChatSettingsId",
                        column: x => x.ChatSettingsId,
                        principalTable: "ChatSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatChannels_ChatSettings_ChatSettingsId1",
                        column: x => x.ChatSettingsId1,
                        principalTable: "ChatSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DreamDaemonSettings",
                columns: table => new
                {
                    AutoStart = table.Column<bool>(nullable: false),
                    AllowWebClient = table.Column<bool>(nullable: false),
                    SoftRestart = table.Column<bool>(nullable: false),
                    SoftShutdown = table.Column<bool>(nullable: false),
                    SecurityLevel = table.Column<int>(nullable: false),
                    PrimaryPort = table.Column<int>(nullable: false),
                    SecondaryPort = table.Column<int>(nullable: false),
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProcessId = table.Column<int>(nullable: true),
                    AccessToken = table.Column<string>(nullable: true),
                    InstanceId = table.Column<long>(nullable: false),
                    CompileJobId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DreamDaemonSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DreamDaemonSettings_CompileJobs_CompileJobId",
                        column: x => x.CompileJobId,
                        principalTable: "CompileJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    AutoCompileInterval = table.Column<int>(nullable: true),
                    TargetDme = table.Column<string>(nullable: true),
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    InstanceId = table.Column<long>(nullable: false),
                    CompileJobId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DreamMakerSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DreamMakerSettings_CompileJobs_CompileJobId",
                        column: x => x.CompileJobId,
                        principalTable: "CompileJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DreamMakerSettings_Instances_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatChannels_ChatSettingsId",
                table: "ChatChannels",
                column: "ChatSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatChannels_ChatSettingsId1",
                table: "ChatChannels",
                column: "ChatSettingsId1");

            migrationBuilder.CreateIndex(
                name: "IX_ChatSettings_InstanceId",
                table: "ChatSettings",
                column: "InstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompileJobs_InstanceId",
                table: "CompileJobs",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_CompileJobs_RevisionInformationId",
                table: "CompileJobs",
                column: "RevisionInformationId");

            migrationBuilder.CreateIndex(
                name: "IX_CompileJobs_TriggeredById",
                table: "CompileJobs",
                column: "TriggeredById");

            migrationBuilder.CreateIndex(
                name: "IX_DreamDaemonSettings_CompileJobId",
                table: "DreamDaemonSettings",
                column: "CompileJobId");

            migrationBuilder.CreateIndex(
                name: "IX_DreamDaemonSettings_InstanceId",
                table: "DreamDaemonSettings",
                column: "InstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DreamMakerSettings_CompileJobId",
                table: "DreamMakerSettings",
                column: "CompileJobId");

            migrationBuilder.CreateIndex(
                name: "IX_DreamMakerSettings_InstanceId",
                table: "DreamMakerSettings",
                column: "InstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstanceUsers_InstanceId",
                table: "InstanceUsers",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_InstanceUsers_UserId",
                table: "InstanceUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_InstanceId",
                table: "Jobs",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_StartedById",
                table: "Jobs",
                column: "StartedById");

            migrationBuilder.CreateIndex(
                name: "IX_RepositorySettings_InstanceId",
                table: "RepositorySettings",
                column: "InstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepositorySettings_RevisionInformationId",
                table: "RepositorySettings",
                column: "RevisionInformationId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionInformations_Revision",
                table: "RevisionInformations",
                column: "Revision",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestMerges_InstanceId",
                table: "TestMerges",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_TestMerges_MergedById",
                table: "TestMerges",
                column: "MergedById");

            migrationBuilder.CreateIndex(
                name: "IX_TestMerges_RevisionInformationId",
                table: "TestMerges",
                column: "RevisionInformationId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SystemIdentifier",
                table: "Users",
                column: "SystemIdentifier",
                unique: true,
                filter: "[SystemIdentifier] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatChannels");

            migrationBuilder.DropTable(
                name: "DreamDaemonSettings");

            migrationBuilder.DropTable(
                name: "DreamMakerSettings");

            migrationBuilder.DropTable(
                name: "InstanceUsers");

            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "RepositorySettings");

            migrationBuilder.DropTable(
                name: "ServerSettings");

            migrationBuilder.DropTable(
                name: "TestMerges");

            migrationBuilder.DropTable(
                name: "ChatSettings");

            migrationBuilder.DropTable(
                name: "CompileJobs");

            migrationBuilder.DropTable(
                name: "Instances");

            migrationBuilder.DropTable(
                name: "RevisionInformations");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
