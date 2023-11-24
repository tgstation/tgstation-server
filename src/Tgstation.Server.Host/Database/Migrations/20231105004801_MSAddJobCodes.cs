using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <inheritdoc />
	public partial class MSAddJobCodes : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);
			migrationBuilder.AddColumn<byte>(
				name: "JobCode",
				table: "Jobs",
				type: "tinyint",
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
