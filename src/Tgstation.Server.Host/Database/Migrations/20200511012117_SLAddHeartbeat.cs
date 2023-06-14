using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Add heartbeat support for SQLite.
	/// </summary>
	public partial class SLAddHeartbeat : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<uint>(
				name: "HeartbeatSeconds",
				table: "DreamDaemonSettings",
				nullable: false,
				defaultValue: 0U);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.RenameTable(
				name: "DreamDaemonSettings",
				newName: "DreamDaemonSettings_down");

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
					AutoStart = table.Column<bool>(nullable: false),
					SoftRestart = table.Column<bool>(nullable: false),
					SoftShutdown = table.Column<bool>(nullable: false),
					ProcessId = table.Column<int>(nullable: true),
					AccessToken = table.Column<string>(nullable: true),
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
				$"INSERT INTO DreamDaemonSettings SELECT Id,AllowWebClient,SecurityLevel,PrimaryPort,SecondaryPort,StartupTimeout,AutoStart,SoftRestart,SoftShutdown,ProcessId,AccessToken,InstanceId FROM DreamDaemonSettings_down");

			migrationBuilder.DropTable(
				name: "DreamDaemonSettings_down");
		}
	}
}
