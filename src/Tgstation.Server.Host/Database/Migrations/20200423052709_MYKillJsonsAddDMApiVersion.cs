using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Drop chat reattach information and add <see cref="Models.ReattachInformationBase.LaunchSecurityLevel"/> for MYSQL.
	/// </summary>
	public partial class MYKillJsonsAddDMApiVersion : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "ChatChannelsJson",
				table: "ReattachInformations");

			migrationBuilder.DropColumn(
				name: "ChatCommandsJson",
				table: "ReattachInformations");

			migrationBuilder.AddColumn<int>(
				name: "LaunchSecurityLevel",
				table: "ReattachInformations",
				nullable: false,
				defaultValue: 1);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "LaunchSecurityLevel",
				table: "ReattachInformations");

			// No default values b/c lol mysql
			migrationBuilder.AddColumn<string>(
				name: "ChatChannelsJson",
				table: "ReattachInformations",
				type: "longtext CHARACTER SET utf8mb4",
				nullable: false);

			migrationBuilder.AddColumn<string>(
				name: "ChatCommandsJson",
				table: "ReattachInformations",
				type: "longtext CHARACTER SET utf8mb4",
				nullable: false);
		}
	}
}
