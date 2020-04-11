using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Reduces column sizes and fixes some broken indexes for MYSQL.
	/// </summary>
	public partial class MYTonsOfValidation : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropIndex(
				name: "IX_ChatBots_InstanceId",
				table: "ChatBots");

			migrationBuilder.DropIndex(
				name: "IX_ChatBots_Name",
				table: "ChatBots");

			migrationBuilder.AlterColumn<string>(
				name: "SystemIdentifier",
				table: "Users",
				nullable: true,
				oldClrType: typeof(string),
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "PullRequestRevision",
				table: "TestMerges",
				maxLength: 40,
				nullable: false,
				oldClrType: typeof(string));

			migrationBuilder.AlterColumn<string>(
				name: "Path",
				table: "Instances",
				maxLength: 10000,
				nullable: false,
				oldClrType: typeof(string));

			migrationBuilder.AlterColumn<string>(
				name: "IrcChannel",
				table: "ChatChannels",
				maxLength: 10000,
				nullable: true,
				oldClrType: typeof(string),
				oldNullable: true);

			migrationBuilder.AlterColumn<int>(
				name: "Provider",
				table: "ChatBots",
				nullable: false,
				oldClrType: typeof(int),
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "Name",
				table: "ChatBots",
				maxLength: 10000,
				nullable: false,
				oldClrType: typeof(string));

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
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropIndex(
				name: "IX_Users_SystemIdentifier",
				table: "Users");

			migrationBuilder.DropIndex(
				name: "IX_ChatBots_InstanceId_Name",
				table: "ChatBots");

			migrationBuilder.AlterColumn<string>(
				name: "SystemIdentifier",
				table: "Users",
				nullable: true,
				oldClrType: typeof(string),
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "PullRequestRevision",
				table: "TestMerges",
				nullable: false,
				oldClrType: typeof(string),
				oldMaxLength: 40);

			migrationBuilder.AlterColumn<string>(
				name: "Path",
				table: "Instances",
				nullable: false,
				oldClrType: typeof(string),
				oldMaxLength: 10000);

			migrationBuilder.AlterColumn<string>(
				name: "IrcChannel",
				table: "ChatChannels",
				nullable: true,
				oldClrType: typeof(string),
				oldMaxLength: 10000,
				oldNullable: true);

			migrationBuilder.AlterColumn<int>(
				name: "Provider",
				table: "ChatBots",
				nullable: true,
				oldClrType: typeof(int));

			migrationBuilder.AlterColumn<string>(
				name: "Name",
				table: "ChatBots",
				nullable: false,
				oldClrType: typeof(string),
				oldMaxLength: 10000);

			migrationBuilder.CreateIndex(
				name: "IX_ChatBots_InstanceId",
				table: "ChatBots",
				column: "InstanceId");

			migrationBuilder.CreateIndex(
				name: "IX_ChatBots_Name",
				table: "ChatBots",
				column: "Name",
				unique: true);
		}
	}
}
