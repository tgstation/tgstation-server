using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Marks the <see cref="CompileJob"/>s of <see cref="ReattachInformation"/>s as non-nullable for MSSQL
	/// </summary>
	public partial class MSReattachCompileJobRequired : Migration
	{
		/// <summary>
		/// Applies the migration
		/// </summary>
		/// <param name="migrationBuilder">The <see cref="MigrationBuilder"/> to use</param>
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

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

		/// <summary>
		/// Unapplies the migration
		/// </summary>
		/// <param name="migrationBuilder">The <see cref="MigrationBuilder"/> to use</param>
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

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
