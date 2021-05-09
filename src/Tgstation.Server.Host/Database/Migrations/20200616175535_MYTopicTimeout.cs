using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Add BYOND topic timeouts for MYSQL.
	/// </summary>
	public partial class MYTopicTimeout : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));
			migrationBuilder.AddColumn<uint>(
				name: "TopicRequestTimeout",
				table: "DreamDaemonSettings",
				nullable: false,
				defaultValue: 0u);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));
			migrationBuilder.DropColumn(
				name: "TopicRequestTimeout",
				table: "DreamDaemonSettings");
		}
	}
}
