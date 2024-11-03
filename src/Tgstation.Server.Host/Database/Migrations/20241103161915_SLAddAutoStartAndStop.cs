using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database
{
	/// <inheritdoc />
	public partial class SLAddAutoStartAndStop : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AlterColumn<long>(
				name: "UserId",
				table: "OAuthConnections",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0L,
				oldClrType: typeof(long),
				oldType: "INTEGER",
				oldNullable: true);

			migrationBuilder.AddColumn<string>(
				name: "AutoStartCron",
				table: "Instances",
				type: "TEXT",
				maxLength: 1000,
				nullable: false,
				defaultValue: String.Empty);

			migrationBuilder.AddColumn<string>(
				name: "AutoStopCron",
				table: "Instances",
				type: "TEXT",
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
				type: "INTEGER",
				nullable: true,
				oldClrType: typeof(long),
				oldType: "INTEGER");
		}
	}
}
