using System;

using Microsoft.EntityFrameworkCore.Migrations;

using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds chat limits for MYSQL.
	/// </summary>
	public partial class MYLimitsOnChat : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AddColumn<ushort>(
				name: "ChatBotLimit",
				table: "Instances",
				nullable: false,
				defaultValue: Instance.DefaultChatBotLimit);

			migrationBuilder.AddColumn<ushort>(
				name: "ChannelLimit",
				table: "ChatBots",
				nullable: false,
				defaultValue: ChatBot.DefaultChannelLimit);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropColumn(
				name: "ChatBotLimit",
				table: "Instances");

			migrationBuilder.DropColumn(
				name: "ChannelLimit",
				table: "ChatBots");
		}
	}
}
