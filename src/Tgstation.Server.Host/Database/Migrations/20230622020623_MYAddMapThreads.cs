using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the MapThreads DreamDaemonSettings column for MYSQL.
	/// </summary>
	public partial class MYAddMapThreads : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<uint>(
				name: "MapThreads",
				table: "DreamDaemonSettings",
				type: "int unsigned",
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
