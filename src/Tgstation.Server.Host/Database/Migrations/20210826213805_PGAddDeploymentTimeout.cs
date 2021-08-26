using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the DreamMakerSettings Timeout column for PostgresSql.
	/// </summary>
	public partial class PGAddDeploymentTimeout : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AddColumn<TimeSpan>(
				name: "Timeout",
				table: "DreamMakerSettings",
				nullable: false,
				defaultValue: TimeSpan.FromHours(1));
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropColumn(
				name: "Timeout",
				table: "DreamMakerSettings");
		}
	}
}
