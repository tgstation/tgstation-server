using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Add DreamDaemon visibilty for MYSQL.
	/// </summary>
	public partial class MYAddDreamDaemonVisibility : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<int>(
				name: "LaunchVisibility",
				table: "ReattachInformations",
				nullable: false,
				defaultValue: 0);

			migrationBuilder.AddColumn<int>(
				name: "Visibility",
				table: "DreamDaemonSettings",
				nullable: false,
				defaultValue: 0);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "LaunchVisibility",
				table: "ReattachInformations");

			migrationBuilder.DropColumn(
				name: "Visibility",
				table: "DreamDaemonSettings");
		}
	}
}
