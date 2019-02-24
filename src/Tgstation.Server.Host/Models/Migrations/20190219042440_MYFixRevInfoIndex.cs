using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Models.Migrations
{
	/// <summary>
	/// Make commit shas non-unique per Instance for MSSQL
	/// </summary>
	public partial class MYFixRevInfoIndex : Migration
	{
		/// <summary>
		/// Applies the migration
		/// </summary>
		/// <param name="migrationBuilder">The <see cref="MigrationBuilder"/> to use</param>
		protected override void Up(MigrationBuilder migrationBuilder)
		{
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

		/// <summary>
		/// Unapplies the migration
		/// </summary>
		/// <param name="migrationBuilder">The <see cref="MigrationBuilder"/> to use</param>
		protected override void Down(MigrationBuilder migrationBuilder)
		{
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
