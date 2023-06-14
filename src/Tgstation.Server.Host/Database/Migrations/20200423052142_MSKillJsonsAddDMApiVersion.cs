using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Drop chat reattach information and add <see cref="Models.ReattachInformationBase.LaunchSecurityLevel"/> for MSSQL.
	/// </summary>
	public partial class MSKillJsonsAddDMApiVersion : Migration
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

			migrationBuilder.AddColumn<string>(
				name: "ChatChannelsJson",
				table: "ReattachInformations",
				type: "nvarchar(max)",
				nullable: false,
				defaultValue: "chat_channels.tgs.json");

			migrationBuilder.AddColumn<string>(
				name: "ChatCommandsJson",
				table: "ReattachInformations",
				type: "nvarchar(max)",
				nullable: false,
				defaultValue: "chat_commands.tgs.json");
		}
	}
}
