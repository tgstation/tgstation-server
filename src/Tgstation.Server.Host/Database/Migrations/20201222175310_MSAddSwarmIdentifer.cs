using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the swarm identifier column for MSSQL.
	/// </summary>
	public partial class MSAddSwarmIdentifer : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

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
				unique: true,
				filter: "[SwarmIdentifer] IS NOT NULL");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

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
