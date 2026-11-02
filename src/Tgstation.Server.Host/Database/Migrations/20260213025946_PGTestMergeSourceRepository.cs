using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Migrations.PostgresSqlDatabase
{
	/// <inheritdoc />
	public partial class PGTestMergeSourceRepository : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<string>(
				name: "SourceRepository",
				table: "TestMerges",
				type: "character varying(100)",
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
