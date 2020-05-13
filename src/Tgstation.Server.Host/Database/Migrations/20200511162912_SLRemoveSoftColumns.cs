using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Removes various defunct columns for SQLite.
	/// </summary>
	public partial class SLRemoveSoftColumns : Migration
	{
		const string MigratedColumns = "Id,AllowWebClient,SecurityLevel,PrimaryPort,SecondaryPort,AutoStart,HeartbeatSeconds,StartupTimeout,InstanceId";

		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.RenameTable(
				name: "DreamDaemonSettings",
				newName: "DreamDaemonSettings_up");

			migrationBuilder.Sql(
				$"CREATE TABLE DreamDaemonSettings AS SELECT {MigratedColumns} FROM DreamDaemonSettings_up");

			migrationBuilder.DropTable(
				name: "DreamDaemonSettings_up");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AddColumn<string>(
				name: "AccessToken",
				table: "DreamDaemonSettings",
				nullable: true);

			migrationBuilder.AddColumn<int>(
				name: "ProcessId",
				table: "DreamDaemonSettings",
				nullable: true);

			migrationBuilder.AddColumn<bool>(
				name: "SoftRestart",
				table: "DreamDaemonSettings",
				nullable: false,
				defaultValue: false);

			migrationBuilder.AddColumn<bool>(
				name: "SoftShutdown",
				table: "DreamDaemonSettings",
				nullable: false,
				defaultValue: false);
		}
	}
}
