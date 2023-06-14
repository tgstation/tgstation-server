using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the DreamMakerSettings DumpOnHeartbeatRestart column for MSSQL.
	/// </summary>
	public partial class MSAddDumpOnHeartbeatRestart : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<bool>(
				name: "DumpOnHeartbeatRestart",
				table: "DreamDaemonSettings",
				nullable: false,
				defaultValue: false);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "DumpOnHeartbeatRestart",
				table: "DreamDaemonSettings");
		}
	}
}
