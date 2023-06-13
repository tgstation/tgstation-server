using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Reduces the index name column size for PostgresSQL.
	/// </summary>
	public partial class PGTruncateInstanceNames : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AlterColumn<string>(
				name: "Name",
				table: "Instances",
				maxLength: 100,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "character varying(10000)",
				oldMaxLength: 10000);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AlterColumn<string>(
				name: "Name",
				table: "Instances",
				type: "character varying(10000)",
				maxLength: 10000,
				nullable: false,
				oldClrType: typeof(string),
				oldMaxLength: 100);
		}
	}
}
