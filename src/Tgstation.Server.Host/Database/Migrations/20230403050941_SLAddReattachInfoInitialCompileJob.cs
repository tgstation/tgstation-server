using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the InitialCompileJobId to the ReattachInformations table for SQLite.
	/// </summary>
	public partial class SLAddReattachInfoInitialCompileJob : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<long>(
				name: "InitialCompileJobId",
				table: "ReattachInformations",
				type: "INTEGER",
				nullable: true);

			migrationBuilder.CreateIndex(
				name: "IX_ReattachInformations_InitialCompileJobId",
				table: "ReattachInformations",
				column: "InitialCompileJobId");

			migrationBuilder.AddForeignKey(
				name: "FK_ReattachInformations_CompileJobs_InitialCompileJobId",
				table: "ReattachInformations",
				column: "InitialCompileJobId",
				principalTable: "CompileJobs",
				principalColumn: "Id");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropForeignKey(
				name: "FK_ReattachInformations_CompileJobs_InitialCompileJobId",
				table: "ReattachInformations");

			migrationBuilder.DropIndex(
				name: "IX_ReattachInformations_InitialCompileJobId",
				table: "ReattachInformations");

			migrationBuilder.DropColumn(
				name: "InitialCompileJobId",
				table: "ReattachInformations");
		}
	}
}
