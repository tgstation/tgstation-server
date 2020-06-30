using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Removes the WatchdogReattachInformations table, SecondaryPort column, and renames the primary port column for SQLite.
	/// </summary>
	public partial class SLRemoveExperimentalWatchdog : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropTable(
				name: "WatchdogReattachInformations");

			migrationBuilder.RenameTable(
				name: "ReattachInformations",
				newName: "ReattachInformations_up");

			migrationBuilder.CreateTable(
				name: "ReattachInformations",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					AccessIdentifier = table.Column<string>(nullable: false),
					ProcessId = table.Column<int>(nullable: false),
					Port = table.Column<ushort>(nullable: false),
					RebootState = table.Column<int>(nullable: false),
					LaunchSecurityLevel = table.Column<int>(nullable: false),
					CompileJobId = table.Column<long>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_ReattachInformations", x => x.Id);
					table.ForeignKey(
						name: "FK_ReattachInformations_CompileJobs_CompileJobId",
						column: x => x.CompileJobId,
						principalTable: "CompileJobs",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.Sql(
				$"INSERT INTO ReattachInformations SELECT Id,AccessIdentifier,ProcessId,Port,RebootState,LaunchSecurityLevel,CompileJobId FROM ReattachInformations_up");

			migrationBuilder.DropTable(
				name: "ReattachInformations_up");

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
					Port = table.Column<ushort>(nullable: false),
					StartupTimeout = table.Column<uint>(nullable: false),
					HeartbeatSeconds = table.Column<uint>(nullable: false),
					AutoStart = table.Column<bool>(nullable: false),
					InstanceId = table.Column<long>(nullable: false)
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
				$"INSERT INTO DreamDaemonSettings SELECT Id,AllowWebClient,SecurityLevel,PrimaryPort,AutoStart,HeartbeatSeconds,StartupTimeout,InstanceId FROM DreamDaemonSettings_up");

			migrationBuilder.DropTable(
				name: "DreamDaemonSettings_up");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.RenameColumn(
				name: "Port",
				table: "DreamDaemonSettings",
				newName: "PrimaryPort");

			migrationBuilder.AddColumn<ushort>(
				name: "SecondaryPort",
				table: "DreamDaemonSettings",
				type: "INTEGER",
				nullable: false,
				defaultValue: (ushort)0);

			migrationBuilder.AddColumn<bool>(
				name: "IsPrimary",
				table: "ReattachInformations",
				type: "INTEGER",
				nullable: false,
				defaultValue: false);

			migrationBuilder.CreateTable(
				name: "WatchdogReattachInformations",
				columns: table => new
				{
					Id = table.Column<long>(type: "INTEGER", nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					AlphaId = table.Column<long>(type: "INTEGER", nullable: true),
					AlphaIsActive = table.Column<bool>(type: "INTEGER", nullable: false),
					BravoId = table.Column<long>(type: "INTEGER", nullable: true),
					InstanceId = table.Column<long>(type: "INTEGER", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_WatchdogReattachInformations", x => x.Id);
					table.ForeignKey(
						name: "FK_WatchdogReattachInformations_ReattachInformations_AlphaId",
						column: x => x.AlphaId,
						principalTable: "ReattachInformations",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
					table.ForeignKey(
						name: "FK_WatchdogReattachInformations_ReattachInformations_BravoId",
						column: x => x.BravoId,
						principalTable: "ReattachInformations",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
					table.ForeignKey(
						name: "FK_WatchdogReattachInformations_Instances_InstanceId",
						column: x => x.InstanceId,
						principalTable: "Instances",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_WatchdogReattachInformations_AlphaId",
				table: "WatchdogReattachInformations",
				column: "AlphaId");

			migrationBuilder.CreateIndex(
				name: "IX_WatchdogReattachInformations_BravoId",
				table: "WatchdogReattachInformations",
				column: "BravoId");

			migrationBuilder.CreateIndex(
				name: "IX_WatchdogReattachInformations_InstanceId",
				table: "WatchdogReattachInformations",
				column: "InstanceId",
				unique: true);
		}
	}
}
