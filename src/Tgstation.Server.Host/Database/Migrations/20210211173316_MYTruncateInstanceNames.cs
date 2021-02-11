using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Reduces the index name column size for MYSQL.
	/// </summary>
	public partial class MYTruncateInstanceNames : Migration
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
				oldType: "longtext CHARACTER SET utf8mb4",
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
				type: "longtext CHARACTER SET utf8mb4",
				maxLength: 10000,
				nullable: false,
				oldClrType: typeof(string),
				oldMaxLength: 100);
		}
	}
}
