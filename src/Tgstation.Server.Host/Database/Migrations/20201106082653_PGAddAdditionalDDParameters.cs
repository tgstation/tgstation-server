using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the AdditionalParameters DD column for PostgresSQL.
	/// </summary>
	public partial class PGAddAdditionalDDParameters : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<string>(
				name: "AdditionalParameters",
				table: "DreamDaemonSettings",
				nullable: false,
				maxLength: 10000,
				defaultValue: String.Empty);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "AdditionalParameters",
				table: "DreamDaemonSettings");
		}
	}
}
