using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <inheritdoc />
	public partial class PGAddDMApiValidationMode : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<int>(
				name: "DMApiValidationMode",
				table: "DreamMakerSettings",
				type: "integer",
				nullable: false,
				defaultValue: 0);

			migrationBuilder.Sql("UPDATE \"DreamMakerSettings\" SET \"DMApiValidationMode\" = \"RequireDMApiValidation\"::int");

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
				type: "boolean",
				nullable: false,
				defaultValue: false);

			migrationBuilder.Sql("UPDATE \"DreamMakerSettings\" SET \"RequireDMApiValidation\" = true WHERE \"DMApiValidationMode\" != 0");
			migrationBuilder.Sql("UPDATE \"DreamMakerSettings\" SET \"RequireDMApiValidation\" = false WHERE \"DMApiValidationMode\" = 0");

			migrationBuilder.DropColumn(
				name: "DMApiValidationMode",
				table: "DreamMakerSettings");
		}
	}
}
