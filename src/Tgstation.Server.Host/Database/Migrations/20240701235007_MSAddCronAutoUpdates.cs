using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <inheritdoc />
	public partial class MSAddCronAutoUpdates : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<string>(
				name: "AutoUpdateCron",
				table: "Instances",
				type: "nvarchar(max)",
				maxLength: 10000,
				nullable: false,
				defaultValue: String.Empty);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "AutoUpdateCron",
				table: "Instances");
		}
	}
}
