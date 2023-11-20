using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <inheritdoc />
	public partial class MYRenameByondColumnsToEngine : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.RenameColumn(
				name: "ByondRights",
				table: "InstancePermissionSets",
				newName: "EngineRights");

			migrationBuilder.RenameColumn(
				name: "ByondVersion",
				table: "CompileJobs",
				newName: "EngineVersion");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.RenameColumn(
				name: "EngineRights",
				table: "InstancePermissionSets",
				newName: "ByondRights");

			migrationBuilder.RenameColumn(
				name: "EngineVersion",
				table: "CompileJobs",
				newName: "ByondVersion");
		}
	}
}
