using System;

using Microsoft.EntityFrameworkCore.Migrations;

using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <inheritdoc />
	public partial class SLAddMinidumpsOption : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			// This was originally minidumps on Linux and full dumps on Windows
			var defaultValue = !new PlatformIdentifier().IsWindows;
			migrationBuilder.AddColumn<bool>(
				name: "Minidumps",
				table: "DreamDaemonSettings",
				type: "INTEGER",
				nullable: false,
				defaultValue: defaultValue);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "Minidumps",
				table: "DreamDaemonSettings");
		}
	}
}
