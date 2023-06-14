using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Renames the Heatbeat DreamDaemonSettings to HealthCheck for MYSQL.
	/// </summary>
	public partial class MYRenameHeartbeatsToHealthChecks : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.RenameColumn(
				name: "HeartbeatSeconds",
				table: "DreamDaemonSettings",
				newName: "HealthCheckSeconds");

			migrationBuilder.RenameColumn(
				name: "DumpOnHeartbeatRestart",
				table: "DreamDaemonSettings",
				newName: "DumpOnHealthCheckRestart");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.RenameColumn(
				name: "HealthCheckSeconds",
				table: "DreamDaemonSettings",
				newName: "HeartbeatSeconds");

			migrationBuilder.RenameColumn(
				name: "DumpOnHealthCheckRestart",
				table: "DreamDaemonSettings",
				newName: "DumpOnHeartbeatRestart");
		}
	}
}
