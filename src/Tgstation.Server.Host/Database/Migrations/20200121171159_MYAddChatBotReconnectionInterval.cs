using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the <see cref="Api.Models.Internal.ChatBotSettings.ReconnectionInterval"/> property for MySQL/MariaDB.
	/// </summary>
	public partial class MYAddChatBotReconnectionInterval : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<long>(
				name: "ReconnectionInterval",
				table: "ChatBots",
				nullable: false,
				defaultValue: 5L);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "ReconnectionInterval",
				table: "ChatBots");
		}
	}
}
