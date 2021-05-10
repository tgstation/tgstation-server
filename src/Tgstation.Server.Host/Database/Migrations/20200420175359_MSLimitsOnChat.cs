using System;

using Microsoft.EntityFrameworkCore.Migrations;

using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds chat limits for MSSQL.
	/// </summary>
	public partial class MSLimitsOnChat : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AddColumn<int>(
				name: "ChatBotLimit",
				table: "Instances",
				nullable: false,
				defaultValue: Instance.DefaultChatBotLimit);

			migrationBuilder.AddColumn<int>(
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
