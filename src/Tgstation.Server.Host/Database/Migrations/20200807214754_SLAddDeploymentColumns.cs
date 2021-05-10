using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds columns for GitHub deployments for SQLite.
	/// </summary>
	public partial class SLAddDeploymentColumns : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AddColumn<bool>(
				name: "CreateGitHubDeployments",
				table: "RepositorySettings",
				nullable: false,
				defaultValue: false);

			migrationBuilder.AddColumn<int>(
				name: "GitHubDeploymentId",
				table: "CompileJobs",
				nullable: true);

			migrationBuilder.AddColumn<long>(
				name: "GitHubRepoId",
				table: "CompileJobs",
				nullable: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.RenameTable(
				name: "RepositorySettings",
				newName: "RepositorySettings_down");

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

			migrationBuilder.Sql(
				$"INSERT INTO RepositorySettings SELECT Id,CommitterName,CommitterEmail,AccessUser,AccessToken,PushTestMergeCommits,ShowTestMergeCommitters,AutoUpdatesKeepTestMerges,AutoUpdatesSynchronize,PostTestMergeComment,InstanceId FROM RepositorySettings_down");

			migrationBuilder.DropTable(
				name: "RepositorySettings_down");

			migrationBuilder.RenameTable(
				name: "CompileJobs",
				newName: "CompileJobs_down");

			migrationBuilder.CreateTable(
				name: "CompileJobs",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					DmeName = table.Column<string>(nullable: false),
					Output = table.Column<string>(nullable: false),
					DirectoryName = table.Column<Guid>(nullable: false),
					MinimumSecurityLevel = table.Column<int>(nullable: true),
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

			migrationBuilder.Sql(
				$"INSERT INTO CompileJobs SELECT Id,DmeName,Output,DirectoryName,MinimumSecurityLevel,JobId,RevisionInformationId,ByondVersion,DMApiMajorVersion,DMApiMinorVersion,DMApiPatchVersion FROM CompileJobs_down");

			migrationBuilder.DropTable(
				name: "CompileJobs_down");
		}
	}
}
