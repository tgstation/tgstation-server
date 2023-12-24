using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the DreamDaemon LogOutput column for PostgresSQL.
	/// </summary>
	public partial class PGAddDreamDaemonLogOutput : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<bool>(
				name: "LogOutput",
				table: "DreamDaemonSettings",
				type: "boolean",
				nullable: false,
				defaultValue: false);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "LogOutput",
				table: "DreamDaemonSettings");
		}
	}
}
