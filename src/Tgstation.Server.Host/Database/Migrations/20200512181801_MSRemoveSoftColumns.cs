using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Removes various defunct columns for MSSQL.
	/// </summary>
	public partial class MSRemoveSoftColumns : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropColumn(
				name: "AccessToken",
				table: "DreamDaemonSettings");

			migrationBuilder.DropColumn(
				name: "ProcessId",
				table: "DreamDaemonSettings");

			migrationBuilder.DropColumn(
				name: "SoftRestart",
				table: "DreamDaemonSettings");

			migrationBuilder.DropColumn(
				name: "SoftShutdown",
				table: "DreamDaemonSettings");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AddColumn<string>(
				name: "AccessToken",
				table: "DreamDaemonSettings",
				type: "nvarchar(max)",
				nullable: true);

			migrationBuilder.AddColumn<int>(
				name: "ProcessId",
				table: "DreamDaemonSettings",
				type: "int",
				nullable: true);

			migrationBuilder.AddColumn<bool>(
				name: "SoftRestart",
				table: "DreamDaemonSettings",
				type: "bit",
				nullable: false,
				defaultValue: false);

			migrationBuilder.AddColumn<bool>(
				name: "SoftShutdown",
				table: "DreamDaemonSettings",
				type: "bit",
				nullable: false,
				defaultValue: false);
		}
	}
}
