using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <inheritdoc />
	public partial class SLAddDMApiValidationMode : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.RenameColumn(
				name: "RequireDMApiValidation",
				table: "DreamMakerSettings",
				newName: "DMApiValidationMode");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.RenameColumn(
				name: "DMApiValidationMode",
				table: "DreamMakerSettings",
				newName: "RequireDMApiValidation");

			migrationBuilder.Sql("UPDATE DreamMakerSettings SET RequireDMApiValidation = 0 WHERE RequireDMApiValidation = 2");
		}
	}
}
