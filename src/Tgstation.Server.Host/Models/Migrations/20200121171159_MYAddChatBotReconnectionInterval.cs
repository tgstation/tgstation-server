using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Models.Migrations
{
	/// <summary>
	/// Adds the <see cref="Api.Models.Internal.ChatBot.ReconnectionInterval"/> property for MySQL/MariaDB.
	/// </summary>
	public partial class MYAddChatBotReconnectionInterval : Migration
	{
		/// <summary>
		/// Applies the migration.
		/// </summary>
		/// <param name="migrationBuilder">The <see cref="MigrationBuilder"/> to use.</param>
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AddColumn<long>(
				name: "ReconnectionInterval",
				table: "ChatBots",
				nullable: false,
				defaultValue: 5L);
		}

		/// <summary>
		/// Unapplies the migration.
		/// </summary>
		/// <param name="migrationBuilder">The <see cref="MigrationBuilder"/> to use.</param>
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropColumn(
				name: "ReconnectionInterval",
				table: "ChatBots");
		}
	}
}
