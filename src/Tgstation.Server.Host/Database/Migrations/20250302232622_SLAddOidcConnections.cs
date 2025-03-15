using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <inheritdoc />
	public partial class SLAddOidcConnections : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.CreateTable(
				name: "OidcConnections",
				columns: table => new
				{
					Id = table.Column<long>(type: "INTEGER", nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					UserId = table.Column<long>(type: "INTEGER", nullable: false),
					SchemeKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
					ExternalUserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_OidcConnections", x => x.Id);
					table.ForeignKey(
						name: "FK_OidcConnections_Users_UserId",
						column: x => x.UserId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_OidcConnections_SchemeKey_ExternalUserId",
				table: "OidcConnections",
				columns: new[] { "SchemeKey", "ExternalUserId" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_OidcConnections_UserId",
				table: "OidcConnections",
				column: "UserId");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropTable(
				name: "OidcConnections");
		}
	}
}
