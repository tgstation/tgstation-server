using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Reduces the index name column size for MSSQL.
	/// </summary>
	public partial class MSTruncateInstanceNames : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AlterColumn<string>(
				name: "Name",
				table: "Instances",
				maxLength: 100,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "nvarchar(max)",
				oldMaxLength: 10000);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AlterColumn<string>(
				name: "Name",
				table: "Instances",
				type: "nvarchar(max)",
				maxLength: 10000,
				nullable: false,
				oldClrType: typeof(string),
				oldMaxLength: 100);
		}
	}
}
