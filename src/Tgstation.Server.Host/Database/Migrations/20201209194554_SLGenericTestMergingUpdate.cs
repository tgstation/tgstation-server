using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Renames the PullRequestRevision and adds the RepositoryOrigin columns for SQLite.
	/// </summary>
	public partial class SLGenericTestMergingUpdate : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.RenameColumn(
				name: "PullRequestRevision",
				table: "TestMerges",
				newName: "TargetCommitSha");

			migrationBuilder.AddColumn<string>(
				name: "RepositoryOrigin",
				table: "CompileJobs",
				nullable: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.RenameColumn(
				name: "TargetCommitSha",
				table: "TestMerges",
				newName: "PullRequestRevision");

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
					GitHubDeploymentId = table.Column<long>(nullable: true),
					GitHubRepoId = table.Column<long>(nullable: true),
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
				$"INSERT INTO CompileJobs SELECT Id,DmeName,Output,DirectoryName,MinimumSecurityLevel,JobId,RevisionInformationId,ByondVersion,DMApiMajorVersion,DMApiMinorVersion,DMApiPatchVersion,GitHubDeploymentId,GitHubRepoId FROM CompileJobs_down");

			migrationBuilder.DropTable(
				name: "CompileJobs_down");
		}
	}
}
