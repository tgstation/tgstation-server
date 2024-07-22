using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <inheritdoc />
	public partial class SLAddOpenDreamTopicPort : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<ushort>(
				name: "OpenDreamTopicPort",
				table: "DreamDaemonSettings",
				type: "INTEGER",
				nullable: false,
				defaultValue: (ushort)0);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "OpenDreamTopicPort",
				table: "DreamDaemonSettings");
		}
	}
}
