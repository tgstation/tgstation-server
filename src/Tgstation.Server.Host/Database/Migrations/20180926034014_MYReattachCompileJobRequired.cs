using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Marks the <see cref="Models.CompileJob"/>s of <see cref="Models.ReattachInformation"/>s as non-nullable for MySQL/MariaDB.
	/// </summary>
	public partial class MYReattachCompileJobRequired : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropForeignKey(
				name: "FK_ReattachInformations_CompileJobs_CompileJobId",
				table: "ReattachInformations");

			migrationBuilder.AlterColumn<long>(
				name: "CompileJobId",
				table: "ReattachInformations",
				nullable: false,
				oldClrType: typeof(long),
				oldNullable: true);

			migrationBuilder.AddForeignKey(
				name: "FK_ReattachInformations_CompileJobs_CompileJobId",
				table: "ReattachInformations",
				column: "CompileJobId",
				principalTable: "CompileJobs",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropForeignKey(
				name: "FK_ReattachInformations_CompileJobs_CompileJobId",
				table: "ReattachInformations");

			migrationBuilder.AlterColumn<long>(
				name: "CompileJobId",
				table: "ReattachInformations",
				nullable: true,
				oldClrType: typeof(long));

			migrationBuilder.AddForeignKey(
				name: "FK_ReattachInformations_CompileJobs_CompileJobId",
				table: "ReattachInformations",
				column: "CompileJobId",
				principalTable: "CompileJobs",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);
		}
	}
}
