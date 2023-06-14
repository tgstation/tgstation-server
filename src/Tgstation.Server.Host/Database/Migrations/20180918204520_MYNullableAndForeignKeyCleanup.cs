using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Cleans up of nullable columns and foreign keys MySQL/MariaDB.
	/// </summary>
	public partial class MYNullableAndForeignKeyCleanup : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropForeignKey(
				name: "FK_Instances_WatchdogReattachInformations_WatchdogReattachInfor~",
				table: "Instances");

			migrationBuilder.DropForeignKey(
				name: "FK_TestMerges_RevisionInformations_PrimaryRevisionInformationId",
				table: "TestMerges");

			migrationBuilder.DropForeignKey(
				name: "FK_CompileJobs_Jobs_JobId",
				table: "CompileJobs");

			migrationBuilder.DropIndex(
				name: "IX_Instances_WatchdogReattachInformationId",
				table: "Instances");

			migrationBuilder.DropIndex(
				name: "IX_CompileJobs_JobId",
				table: "CompileJobs");

			migrationBuilder.DropColumn(
				name: "WatchdogReattachInformationId",
				table: "Instances");

			migrationBuilder.AddColumn<long>(
				name: "InstanceId",
				table: "WatchdogReattachInformations",
				nullable: false,
				defaultValue: 0L);

			migrationBuilder.AlterColumn<long>(
				name: "PrimaryRevisionInformationId",
				table: "TestMerges",
				nullable: false,
				oldClrType: typeof(long),
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "Output",
				table: "CompileJobs",
				nullable: false,
				oldClrType: typeof(string),
				oldNullable: true);

			migrationBuilder.AlterColumn<long>(
				name: "JobId",
				table: "CompileJobs",
				nullable: false,
				oldClrType: typeof(long),
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "DmeName",
				table: "CompileJobs",
				nullable: false,
				oldClrType: typeof(string),
				oldNullable: true);

			migrationBuilder.AlterColumn<Guid>(
				name: "DirectoryName",
				table: "CompileJobs",
				nullable: false,
				oldClrType: typeof(Guid),
				oldNullable: true);

			migrationBuilder.CreateIndex(
				name: "IX_WatchdogReattachInformations_InstanceId",
				table: "WatchdogReattachInformations",
				column: "InstanceId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_CompileJobs_JobId",
				table: "CompileJobs",
				column: "JobId",
				unique: true);

			migrationBuilder.AddForeignKey(
				name: "FK_CompileJobs_Jobs_JobId",
				table: "CompileJobs",
				column: "JobId",
				principalTable: "Jobs",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);

			migrationBuilder.AddForeignKey(
				name: "FK_TestMerges_RevisionInformations_PrimaryRevisionInformationId",
				table: "TestMerges",
				column: "PrimaryRevisionInformationId",
				principalTable: "RevisionInformations",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);

			migrationBuilder.AddForeignKey(
				name: "FK_WatchdogReattachInformations_Instances_InstanceId",
				table: "WatchdogReattachInformations",
				column: "InstanceId",
				principalTable: "Instances",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropForeignKey(
				name: "FK_TestMerges_RevisionInformations_PrimaryRevisionInformationId",
				table: "TestMerges");

			migrationBuilder.DropForeignKey(
				name: "FK_WatchdogReattachInformations_Instances_InstanceId",
				table: "WatchdogReattachInformations");

			migrationBuilder.DropForeignKey(
				name: "FK_CompileJobs_Jobs_JobId",
				table: "CompileJobs");

			migrationBuilder.DropIndex(
				name: "IX_WatchdogReattachInformations_InstanceId",
				table: "WatchdogReattachInformations");

			migrationBuilder.DropIndex(
				name: "IX_CompileJobs_JobId",
				table: "CompileJobs");

			migrationBuilder.DropColumn(
				name: "InstanceId",
				table: "WatchdogReattachInformations");

			migrationBuilder.AlterColumn<long>(
				name: "PrimaryRevisionInformationId",
				table: "TestMerges",
				nullable: true,
				oldClrType: typeof(long));

			migrationBuilder.AddColumn<long>(
				name: "WatchdogReattachInformationId",
				table: "Instances",
				nullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "Output",
				table: "CompileJobs",
				nullable: true,
				oldClrType: typeof(string));

			migrationBuilder.AlterColumn<long>(
				name: "JobId",
				table: "CompileJobs",
				nullable: true,
				oldClrType: typeof(long));

			migrationBuilder.AlterColumn<string>(
				name: "DmeName",
				table: "CompileJobs",
				nullable: true,
				oldClrType: typeof(string));

			migrationBuilder.AlterColumn<Guid>(
				name: "DirectoryName",
				table: "CompileJobs",
				nullable: true,
				oldClrType: typeof(Guid));

			migrationBuilder.CreateIndex(
				name: "IX_Instances_WatchdogReattachInformationId",
				table: "Instances",
				column: "WatchdogReattachInformationId");

			migrationBuilder.CreateIndex(
				name: "IX_CompileJobs_JobId",
				table: "CompileJobs",
				column: "JobId");

			migrationBuilder.AddForeignKey(
				name: "FK_CompileJobs_Jobs_JobId",
				table: "CompileJobs",
				column: "JobId",
				principalTable: "Jobs",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);

			migrationBuilder.AddForeignKey(
				name: "FK_Instances_WatchdogReattachInformations_WatchdogReattachInfor~",
				table: "Instances",
				column: "WatchdogReattachInformationId",
				principalTable: "WatchdogReattachInformations",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);

			migrationBuilder.AddForeignKey(
				name: "FK_TestMerges_RevisionInformations_PrimaryRevisionInformationId",
				table: "TestMerges",
				column: "PrimaryRevisionInformationId",
				principalTable: "RevisionInformations",
				principalColumn: "Id",
				onDelete: ReferentialAction.SetNull);
		}
	}
}
