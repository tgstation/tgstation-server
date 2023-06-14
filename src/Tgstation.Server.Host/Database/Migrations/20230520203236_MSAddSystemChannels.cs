using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the IsSystemChannel chat channel option for MSSQL.
	/// </summary>
	public partial class MSAddSystemChannels : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<bool>(
				name: "IsSystemChannel",
				table: "ChatChannels",
				type: "bit",
				nullable: false,
				defaultValue: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "IsSystemChannel",
				table: "ChatChannels");
		}
	}
}
