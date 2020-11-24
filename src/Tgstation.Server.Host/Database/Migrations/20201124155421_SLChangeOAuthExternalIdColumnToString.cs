using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Changes the OAuthConnections ExternalUserId column to a string for SQLite.
	/// </summary>
	public partial class SLChangeOAuthExternalIdColumnToString : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AlterColumn<string>(
				name: "ExternalUserId",
				table: "OAuthConnections",
				maxLength: 100,
				nullable: false,
				oldClrType: typeof(ulong),
				oldType: "INTEGER");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AlterColumn<ulong>(
				name: "ExternalUserId",
				table: "OAuthConnections",
				type: "INTEGER",
				nullable: false,
				oldClrType: typeof(string),
				oldMaxLength: 100);
		}
	}
}
