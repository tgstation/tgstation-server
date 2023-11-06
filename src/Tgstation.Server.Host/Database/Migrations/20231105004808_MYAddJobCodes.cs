using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <inheritdoc />
	public partial class MYAddJobCodes : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);
			migrationBuilder.AddColumn<byte>(
				name: "JobCode",
				table: "Jobs",
				type: "tinyint unsigned",
				nullable: false,
				defaultValue: (byte)0);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);
			migrationBuilder.DropColumn(
				name: "JobCode",
				table: "Jobs");
		}
	}
}
