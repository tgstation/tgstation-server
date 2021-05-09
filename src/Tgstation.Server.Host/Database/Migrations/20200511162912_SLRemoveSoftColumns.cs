using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Removes various defunct columns for SQLite.
	/// </summary>
	public partial class SLRemoveSoftColumns : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.RenameTable(
				name: "DreamDaemonSettings",
				newName: "DreamDaemonSettings_up");

			migrationBuilder.CreateTable(
				name: "DreamDaemonSettings",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					AllowWebClient = table.Column<bool>(nullable: false),
					SecurityLevel = table.Column<int>(nullable: false),
					PrimaryPort = table.Column<ushort>(nullable: false),
					SecondaryPort = table.Column<ushort>(nullable: false),
					StartupTimeout = table.Column<uint>(nullable: false),
					HeartbeatSeconds = table.Column<uint>(nullable: false),
					AutoStart = table.Column<bool>(nullable: false),
					InstanceId = table.Column<long>(nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_DreamDaemonSettings", x => x.Id);
					table.ForeignKey(
						name: "FK_DreamDaemonSettings_Instances_InstanceId",
						column: x => x.InstanceId,
						principalTable: "Instances",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.Sql(
				$"INSERT INTO DreamDaemonSettings SELECT Id,AllowWebClient,SecurityLevel,PrimaryPort,SecondaryPort,AutoStart,HeartbeatSeconds,StartupTimeout,InstanceId FROM DreamDaemonSettings_up");

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
