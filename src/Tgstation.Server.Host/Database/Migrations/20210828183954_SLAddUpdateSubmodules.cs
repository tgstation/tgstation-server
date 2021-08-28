using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the UpdateSubmodules repository setting for SQLite.
	/// </summary>
	public partial class SLAddUpdateSubmodules : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AddColumn<bool>(
				name: "UpdateSubmodules",
				table: "RepositorySettings",
				nullable: false,
				defaultValue: true);
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
					CreateGitHubDeployments = table.Column<bool>(nullable: false),
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
				$"INSERT INTO RepositorySettings SELECT Id,CommitterName,CommitterEmail,AccessUser,AccessToken,PushTestMergeCommits,ShowTestMergeCommitters,AutoUpdatesKeepTestMerges,AutoUpdatesSynchronize,PostTestMergeComment,InstanceId,CreateGitHubDeployments FROM RepositorySettings_down");

			migrationBuilder.DropTable(
				name: "RepositorySettings_down");

			migrationBuilder.RenameTable(
				name: "RepositorySettings",
				newName: "RepositorySettings_down");

			migrationBuilder.RenameTable(
				name: "RepositorySettings_down",
				newName: "RepositorySettings");
		}
	}
}
