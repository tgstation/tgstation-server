using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the swarm identifier column for PostgresSQL.
	/// </summary>
	public partial class PGAddSwarmIdentifer : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropIndex(
				name: "IX_Instances_Path",
				table: "Instances");

			migrationBuilder.AddColumn<string>(
				name: "SwarmIdentifer",
				table: "Instances",
				nullable: true);

			migrationBuilder.CreateIndex(
				name: "IX_Instances_Path_SwarmIdentifer",
				table: "Instances",
				columns: new[] { "Path", "SwarmIdentifer" },
				unique: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropIndex(
				name: "IX_Instances_Path_SwarmIdentifer",
				table: "Instances");

			migrationBuilder.DropColumn(
				name: "SwarmIdentifer",
				table: "Instances");

			migrationBuilder.CreateIndex(
				name: "IX_Instances_Path",
				table: "Instances",
				column: "Path",
				unique: true);
		}
	}
}
