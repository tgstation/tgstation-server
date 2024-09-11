using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <inheritdoc />
	public partial class MYAddDMApiValidationMode : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<int>(
				name: "DMApiValidationMode",
				table: "DreamMakerSettings",
				type: "int",
				nullable: false,
				defaultValue: 0);

			migrationBuilder.Sql("UPDATE DreamMakerSettings SET DMApiValidationMode = RequireDMApiValidation");

			migrationBuilder.DropColumn(
				name: "RequireDMApiValidation",
				table: "DreamMakerSettings");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<bool>(
				name: "RequireDMApiValidation",
				table: "DreamMakerSettings",
				type: "tinyint(1)",
				nullable: false,
				defaultValue: false);

			migrationBuilder.Sql("UPDATE DreamMakerSettings SET RequireDMApiValidation = 1 WHERE DMApiValidationMode != 0");
			migrationBuilder.Sql("UPDATE DreamMakerSettings SET RequireDMApiValidation = 0 WHERE DMApiValidationMode = 0");

			migrationBuilder.DropColumn(
				name: "DMApiValidationMode",
				table: "DreamMakerSettings");
		}
	}
}
