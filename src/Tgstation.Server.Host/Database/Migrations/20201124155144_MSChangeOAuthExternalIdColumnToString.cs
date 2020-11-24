using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Changes the OAuthConnections ExternalUserId column to a string for MSSQL.
	/// </summary>
	public partial class MSChangeOAuthExternalIdColumnToString : Migration
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
				oldClrType: typeof(decimal),
				oldType: "decimal(20,0)");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AlterColumn<decimal>(
				name: "ExternalUserId",
				table: "OAuthConnections",
				type: "decimal(20,0)",
				nullable: false,
				oldClrType: typeof(string),
				oldMaxLength: 100);
		}
	}
}
