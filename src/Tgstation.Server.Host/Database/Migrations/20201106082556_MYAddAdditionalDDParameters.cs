using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the AdditionalParameters DD column for MySQL.
	/// </summary>
	public partial class MYAddAdditionalDDParameters : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AddColumn<string>(
				name: "AdditionalParameters",
				table: "DreamDaemonSettings",
				maxLength: 10000);

			// Special handling to get our NOT NULL constraint
			migrationBuilder.Sql("UPDATE DreamDaemonSettings SET AdditionalParameters = \"\"");

			migrationBuilder.AlterColumn<string>(
				name: "AdditionalParameters",
				table: "DreamDaemonSettings",
				nullable: false,
				maxLength: 10000);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropColumn(
				name: "AdditionalParameters",
				table: "DreamDaemonSettings");
		}
	}
}
