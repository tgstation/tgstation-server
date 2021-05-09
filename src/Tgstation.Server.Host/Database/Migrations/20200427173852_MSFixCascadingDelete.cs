using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Fix cascading data deletes for <see cref="Models.Instance"/>s on MSSQL.
	/// </summary>
	public partial class MSFixCascadingDelete : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropForeignKey(
				name: "FK_CompileJobs_Jobs_JobId",
				table: "CompileJobs");

			migrationBuilder.DropForeignKey(
				name: "FK_CompileJobs_RevisionInformations_RevisionInformationId",
				table: "CompileJobs");

			migrationBuilder.DropForeignKey(
				name: "FK_RevInfoTestMerges_TestMerges_TestMergeId",
				table: "RevInfoTestMerges");

			migrationBuilder.DropForeignKey(
				name: "FK_TestMerges_RevisionInformations_PrimaryRevisionInformationId",
				table: "TestMerges");

			migrationBuilder.AddForeignKey(
				name: "FK_CompileJobs_Jobs_JobId",
				table: "CompileJobs",
				column: "JobId",
				principalTable: "Jobs",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);

			migrationBuilder.AddForeignKey(
				name: "FK_CompileJobs_RevisionInformations_RevisionInformationId",
				table: "CompileJobs",
				column: "RevisionInformationId",
				principalTable: "RevisionInformations",
				principalColumn: "Id");

			migrationBuilder.AddForeignKey(
				name: "FK_RevInfoTestMerges_TestMerges_TestMergeId",
				table: "RevInfoTestMerges",
				column: "TestMergeId",
				principalTable: "TestMerges",
				principalColumn: "Id");

			migrationBuilder.AddForeignKey(
				name: "FK_TestMerges_RevisionInformations_PrimaryRevisionInformationId",
				table: "TestMerges",
				column: "PrimaryRevisionInformationId",
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
				name: "FK_CompileJobs_Jobs_JobId",
				table: "CompileJobs");

			migrationBuilder.DropForeignKey(
				name: "FK_CompileJobs_RevisionInformations_RevisionInformationId",
				table: "CompileJobs");

			migrationBuilder.DropForeignKey(
				name: "FK_RevInfoTestMerges_TestMerges_TestMergeId",
				table: "RevInfoTestMerges");

			migrationBuilder.DropForeignKey(
				name: "FK_TestMerges_RevisionInformations_PrimaryRevisionInformationId",
				table: "TestMerges");

			migrationBuilder.AddForeignKey(
				name: "FK_CompileJobs_Jobs_JobId",
				table: "CompileJobs",
				column: "JobId",
				principalTable: "Jobs",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);

			migrationBuilder.AddForeignKey(
				name: "FK_CompileJobs_RevisionInformations_RevisionInformationId",
				table: "CompileJobs",
				column: "RevisionInformationId",
				principalTable: "RevisionInformations",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);

			migrationBuilder.AddForeignKey(
				name: "FK_RevInfoTestMerges_TestMerges_TestMergeId",
				table: "RevInfoTestMerges",
				column: "TestMergeId",
				principalTable: "TestMerges",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);

			migrationBuilder.AddForeignKey(
				name: "FK_TestMerges_RevisionInformations_PrimaryRevisionInformationId",
				table: "TestMerges",
				column: "PrimaryRevisionInformationId",
				principalTable: "RevisionInformations",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);
		}
	}
}
