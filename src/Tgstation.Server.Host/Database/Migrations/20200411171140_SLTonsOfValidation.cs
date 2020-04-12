using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Reduces column sizes and fixes some broken indexes for SQLite.
	/// </summary>
	public partial class SLTonsOfValidation : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropForeignKey(
				name: "FK_ChatBots_Instances_InstanceId",
				table: "ChatBots");

			migrationBuilder.DropIndex(
				name: "IX_ChatChannels_ChatSettingsId_IrcChannel",
				table: "ChatChannels");

			migrationBuilder.DropIndex(
				name: "IX_ChatBots_InstanceId",
				table: "ChatBots");

			migrationBuilder.DropIndex(
				name: "IX_ChatBots_Name",
				table: "ChatBots");

			migrationBuilder.AlterColumn<int>(
				name: "Provider",
				table: "ChatBots",
				nullable: false,
				oldClrType: typeof(int),
				oldNullable: true);

			migrationBuilder.CreateIndex(
				name: "IX_Users_SystemIdentifier",
				table: "Users",
				column: "SystemIdentifier",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_ChatBots_InstanceId_Name",
				table: "ChatBots",
				columns: new[] { "InstanceId", "Name" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_ChatChannels_ChatSettingsId_IrcChannel",
				table: "ChatChannels",
				columns: new[] { "ChatSettingsId", "IrcChannel" },
				unique: true,
				filter: "[IrcChannel] IS NOT NULL");

			migrationBuilder.AddForeignKey(
				name: "FK_ChatBots_Instances_InstanceId",
				table: "ChatBots",
				column: "InstanceId",
				principalTable: "Instances",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropForeignKey(
				name: "FK_ChatBots_Instances_InstanceId",
				table: "ChatBots");

			migrationBuilder.DropIndex(
				name: "IX_ChatChannels_ChatSettingsId_IrcChannel",
				table: "ChatChannels");

			migrationBuilder.DropIndex(
				name: "IX_Users_SystemIdentifier",
				table: "Users");

			migrationBuilder.DropIndex(
				name: "IX_ChatBots_InstanceId_Name",
				table: "ChatBots");

			migrationBuilder.AlterColumn<int>(
				name: "Provider",
				table: "ChatBots",
				nullable: true,
				oldClrType: typeof(int));

			migrationBuilder.CreateIndex(
				name: "IX_ChatBots_InstanceId",
				table: "ChatBots",
				column: "InstanceId");

			migrationBuilder.CreateIndex(
				name: "IX_ChatBots_Name",
				table: "ChatBots",
				column: "Name",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_ChatChannels_ChatSettingsId_IrcChannel",
				table: "ChatChannels",
				columns: new[] { "ChatSettingsId", "IrcChannel" },
				unique: true,
				filter: "[IrcChannel] IS NOT NULL");

			migrationBuilder.AddForeignKey(
				name: "FK_ChatBots_Instances_InstanceId",
				table: "ChatBots",
				column: "InstanceId",
				principalTable: "Instances",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}
	}
}
