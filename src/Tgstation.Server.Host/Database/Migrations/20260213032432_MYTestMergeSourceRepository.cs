using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Migrations.MySqlDatabase
{
	/// <inheritdoc />
	public partial class MYTestMergeSourceRepository : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<string>(
				name: "SourceRepository",
				table: "TestMerges",
				type: "varchar(100)",
				maxLength: 100,
				nullable: true)
				.Annotation("MySql:CharSet", "utf8mb4");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "SourceRepository",
				table: "TestMerges");
		}
	}
}
