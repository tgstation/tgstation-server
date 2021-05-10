using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Removes the WatchdogReattachInformations table, SecondaryPort column, and renames the primary port column for MSSQL.
	/// </summary>
	public partial class MSRemoveExperimentalWatchdog : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropTable(
				name: "WatchdogReattachInformations");

			migrationBuilder.DropColumn(
				name: "IsPrimary",
				table: "ReattachInformations");

			migrationBuilder.DropColumn(
				name: "SecondaryPort",
				table: "DreamDaemonSettings");

			migrationBuilder.RenameColumn(
				name: "PrimaryPort",
				table: "DreamDaemonSettings",
				newName: "Port");
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

			migrationBuilder.AddColumn<int>(
				name: "SecondaryPort",
				table: "DreamDaemonSettings",
				type: "int",
				nullable: false,
				defaultValue: 1338);

			migrationBuilder.AddColumn<bool>(
				name: "IsPrimary",
				table: "ReattachInformations",
				type: "bit",
				nullable: false,
				defaultValue: true);

			migrationBuilder.CreateTable(
				name: "WatchdogReattachInformations",
				columns: table => new
				{
					Id = table.Column<long>(type: "bigint", nullable: false)
						.Annotation("SqlServer:Identity", "1, 1"),
					AlphaId = table.Column<long>(type: "bigint", nullable: true),
					AlphaIsActive = table.Column<bool>(type: "bit", nullable: false),
					BravoId = table.Column<long>(type: "bigint", nullable: true),
					InstanceId = table.Column<long>(type: "bigint", nullable: false),
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
