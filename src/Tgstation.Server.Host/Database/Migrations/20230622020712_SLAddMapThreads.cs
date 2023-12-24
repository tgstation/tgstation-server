using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the MapThreads DreamDaemonSettings column for SQLite.
	/// </summary>
	public partial class SLAddMapThreads : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<uint>(
				name: "MapThreads",
				table: "DreamDaemonSettings",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0u);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "MapThreads",
				table: "DreamDaemonSettings");
		}
	}
}
