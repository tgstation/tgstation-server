using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Add heartbeat support for MYSQL.
	/// </summary>
	public partial class MYAddHeartbeat : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<uint>(
				name: "HeartbeatSeconds",
				table: "DreamDaemonSettings",
				nullable: false,
				defaultValue: 0u);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "HeartbeatSeconds",
				table: "DreamDaemonSettings");
		}
	}
}
