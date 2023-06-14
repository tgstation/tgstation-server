using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the DreamMakerSettings Timeout column for MYSQL.
	/// </summary>
	public partial class MYAddDeploymentTimeout : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<TimeSpan>(
				name: "Timeout",
				table: "DreamMakerSettings",
				nullable: false,
				defaultValue: TimeSpan.FromHours(1));
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "Timeout",
				table: "DreamMakerSettings");
		}
	}
}
