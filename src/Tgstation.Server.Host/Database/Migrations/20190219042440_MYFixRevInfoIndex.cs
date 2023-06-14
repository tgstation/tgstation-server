using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Make commit shas non-unique per Instance for MySQL/MariaDB.
	/// </summary>
	public partial class MYFixRevInfoIndex : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropForeignKey(
				name: "FK_RevisionInformations_Instances_InstanceId",
				table: "RevisionInformations");

			migrationBuilder.DropIndex(
				name: "IX_RevisionInformations_CommitSha",
				table: "RevisionInformations");

			migrationBuilder.DropIndex(
				name: "IX_RevisionInformations_InstanceId",
				table: "RevisionInformations");

			migrationBuilder.CreateIndex(
				name: "IX_RevisionInformations_InstanceId_CommitSha",
				table: "RevisionInformations",
				columns: new[] { "InstanceId", "CommitSha" },
				unique: true);

			migrationBuilder.AddForeignKey(
				name: "FK_RevisionInformations_Instances_InstanceId",
				table: "RevisionInformations",
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
				name: "FK_RevisionInformations_Instances_InstanceId",
				table: "RevisionInformations");

			migrationBuilder.DropIndex(
				name: "IX_RevisionInformations_InstanceId_CommitSha",
				table: "RevisionInformations");

			migrationBuilder.CreateIndex(
				name: "IX_RevisionInformations_CommitSha",
				table: "RevisionInformations",
				column: "CommitSha",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_RevisionInformations_InstanceId",
				table: "RevisionInformations",
				column: "InstanceId");

			migrationBuilder.AddForeignKey(
				name: "FK_RevisionInformations_Instances_InstanceId",
				table: "RevisionInformations",
				column: "InstanceId",
				principalTable: "Instances",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}
	}
}
