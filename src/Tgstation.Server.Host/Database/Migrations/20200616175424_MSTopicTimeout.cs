using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Add BYOND topic timeouts for MSSQL.
	/// </summary>
	public partial class MSTopicTimeout : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);
			migrationBuilder.AddColumn<long>(
				name: "TopicRequestTimeout",
				table: "DreamDaemonSettings",
				nullable: false,
				defaultValue: 0L);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);
			migrationBuilder.DropColumn(
				name: "TopicRequestTimeout",
				table: "DreamDaemonSettings");
		}
	}
}
