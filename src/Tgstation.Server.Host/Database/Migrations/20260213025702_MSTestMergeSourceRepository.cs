using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Migrations.SqlServerDatabase
{
	/// <inheritdoc />
	public partial class MSTestMergeSourceRepository : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<string>(
				name: "SourceRepository",
				table: "TestMerges",
				type: "nvarchar(100)",
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
