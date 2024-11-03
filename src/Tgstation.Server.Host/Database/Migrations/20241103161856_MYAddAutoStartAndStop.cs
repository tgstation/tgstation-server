using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database
{
	/// <inheritdoc />
	public partial class MYAddAutoStartAndStop : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AlterColumn<long>(
				name: "UserId",
				table: "OAuthConnections",
				type: "bigint",
				nullable: false,
				defaultValue: 0L,
				oldClrType: typeof(long),
				oldType: "bigint",
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "AutoUpdateCron",
				table: "Instances",
				type: "varchar(1000)",
				maxLength: 1000,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "varchar(10000)",
				oldMaxLength: 10000)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AddColumn<string>(
				name: "AutoStartCron",
				table: "Instances",
				type: "varchar(1000)",
				maxLength: 1000,
				nullable: false,
				defaultValue: String.Empty)
				.Annotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AddColumn<string>(
				name: "AutoStopCron",
				table: "Instances",
				type: "varchar(1000)",
				maxLength: 1000,
				nullable: false,
				defaultValue: String.Empty)
				.Annotation("MySql:CharSet", "utf8mb4");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "AutoStartCron",
				table: "Instances");

			migrationBuilder.DropColumn(
				name: "AutoStopCron",
				table: "Instances");

			migrationBuilder.AlterColumn<long>(
				name: "UserId",
				table: "OAuthConnections",
				type: "bigint",
				nullable: true,
				oldClrType: typeof(long),
				oldType: "bigint");

			migrationBuilder.AlterColumn<string>(
				name: "AutoUpdateCron",
				table: "Instances",
				type: "varchar(10000)",
				maxLength: 10000,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "varchar(1000)",
				oldMaxLength: 1000)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");
		}
	}
}
