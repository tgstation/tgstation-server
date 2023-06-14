using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the DreamMakerSettings Timeout column for SQLite.
	/// </summary>
	public partial class SLAddDeploymentTimeout : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<TimeSpan>(
				name: "Timeout",
				table: "DreamMakerSettings",
				nullable: false,
				defaultValue: TimeSpan.FromHours(1));
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.RenameTable(
				name: "DreamMakerSettings",
				newName: "DreamMakerSettings_down");

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
					RequireDMApiValidation = table.Column<bool>(nullable: false),
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

			migrationBuilder.Sql(
				$"INSERT INTO DreamMakerSettings SELECT Id,ProjectName,ApiValidationPort,ApiValidationSecurityLevel,InstanceId,RequireDMApiValidation FROM DreamMakerSettings_down");

			migrationBuilder.DropTable(
				name: "DreamMakerSettings_down");

			migrationBuilder.RenameTable(
				name: "DreamMakerSettings",
				newName: "DreamMakerSettings_down");

			migrationBuilder.RenameTable(
				name: "DreamMakerSettings_down",
				newName: "DreamMakerSettings");
		}
	}
}
