using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Models.Migrations
{
	/// <summary>
	/// Make commit shas non-unique per Instance for MSSQL
	/// </summary>
	public partial class MSFixRevInfoIndex : Migration
	{
		/// <summary>
		/// Applies the migration
		/// </summary>
		/// <param name="migrationBuilder">The <see cref="MigrationBuilder"/> to use</param>
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

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
		}

		/// <summary>
		/// Unpplies the migration
		/// </summary>
		/// <param name="migrationBuilder">The <see cref="MigrationBuilder"/> to use</param>
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

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
		}
	}
}
