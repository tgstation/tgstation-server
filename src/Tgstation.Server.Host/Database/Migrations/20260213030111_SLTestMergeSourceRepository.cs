using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Migrations.SqliteDatabase
{
	/// <inheritdoc />
	public partial class SLTestMergeSourceRepository : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<string>(
				name: "SourceRepository",
				table: "TestMerges",
				type: "TEXT",
				maxLength: 100,
				nullable: true);
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
