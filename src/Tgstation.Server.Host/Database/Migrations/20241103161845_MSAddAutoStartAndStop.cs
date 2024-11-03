using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database
{
	/// <inheritdoc />
	public partial class MSAddAutoStartAndStop : Migration
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
				type: "nvarchar(1000)",
				maxLength: 1000,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "nvarchar(max)",
				oldMaxLength: 10000);

			migrationBuilder.AddColumn<string>(
				name: "AutoStartCron",
				table: "Instances",
				type: "nvarchar(1000)",
				maxLength: 1000,
				nullable: false,
				defaultValue: String.Empty);

			migrationBuilder.AddColumn<string>(
				name: "AutoStopCron",
				table: "Instances",
				type: "nvarchar(1000)",
				maxLength: 1000,
				nullable: false,
				defaultValue: String.Empty);
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
				type: "nvarchar(max)",
				maxLength: 10000,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "nvarchar(1000)",
				oldMaxLength: 1000);
		}
	}
}
