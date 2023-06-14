using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the UpdateSubmodules repository setting for MSSQL.
	/// </summary>
	public partial class MSAddUpdateSubmodules : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<bool>(
				name: "UpdateSubmodules",
				table: "RepositorySettings",
				nullable: false,
				defaultValue: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "UpdateSubmodules",
				table: "RepositorySettings");
		}
	}
}
