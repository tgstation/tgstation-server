using Microsoft.EntityFrameworkCore.Migrations;
using System;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Update models for making the DMAPI optional for SQLite.
	/// </summary>
	public partial class SLAllowNullDMApi : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AddColumn<bool>(
				name: "RequireDMApiValidation",
				table: "DreamMakerSettings",
				nullable: false,
				defaultValue: true);

			migrationBuilder.RenameTable(
				name: "CompileJobs",
				newName: "CompileJobs_up");

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
					DMApiPatchVersion = table.Column<int>(nullable: true)
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
				$"INSERT INTO CompileJobs SELECT * FROM CompileJobs_up");

			migrationBuilder.DropTable(
				name: "CompileJobs_up");

			// SQLite HATES this migration for some reason and thinks CompileJobs_up still exists???
			migrationBuilder.RenameTable(
				name: "CompileJobs",
				newName: "CompileJobs_up");

			migrationBuilder.RenameTable(
				name: "CompileJobs_up",
				newName: "CompileJobs");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

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
					MinimumSecurityLevel = table.Column<int>(nullable: false, defaultValue: DreamDaemonSecurity.Ultrasafe),
					JobId = table.Column<long>(nullable: false),
					RevisionInformationId = table.Column<long>(nullable: false),
					ByondVersion = table.Column<string>(nullable: false),
					DMApiMajorVersion = table.Column<int>(nullable: true),
					DMApiMinorVersion = table.Column<int>(nullable: true),
					DMApiPatchVersion = table.Column<int>(nullable: true)
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
				$"INSERT INTO CompileJobs SELECT * FROM CompileJobs_down");

			migrationBuilder.DropTable(
				name: "CompileJobs_down");

			migrationBuilder.RenameTable(
				name: "CompileJobs",
				newName: "CompileJobs_down");

			migrationBuilder.RenameTable(
				name: "CompileJobs_down",
				newName: "CompileJobs");

			migrationBuilder.DropColumn(
				name: "RequireDMApiValidation",
				table: "DreamMakerSettings");
		}
	}
}
