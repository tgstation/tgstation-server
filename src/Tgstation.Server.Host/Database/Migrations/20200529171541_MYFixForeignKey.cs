using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Fix the CompileJob/RevisionInformation foreign key for MySQL.
	/// </summary>
	public partial class MYFixForeignKey : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropForeignKey(
				name: "FK_CompileJobs_RevisionInformations_RevisionInformationId",
				table: "CompileJobs");

			migrationBuilder.AddForeignKey(
				name: "FK_CompileJobs_RevisionInformations_RevisionInformationId",
				table: "CompileJobs",
				column: "RevisionInformationId",
				principalTable: "RevisionInformations",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropForeignKey(
				name: "FK_CompileJobs_RevisionInformations_RevisionInformationId",
				table: "CompileJobs");

			migrationBuilder.AddForeignKey(
				name: "FK_CompileJobs_RevisionInformations_RevisionInformationId",
				table: "CompileJobs",
				column: "RevisionInformationId",
				principalTable: "RevisionInformations",
				principalColumn: "Id");
		}
	}
}
