using System;

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the OAuthConnections table for MYSQL.
	/// </summary>
	public partial class MYAddOAuthConnections : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.CreateTable(
				name: "OAuthConnections",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
					Provider = table.Column<int>(nullable: false),
					ExternalUserId = table.Column<string>(nullable: false, maxLength: 100),
					UserId = table.Column<long>(nullable: true),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_OAuthConnections", x => x.Id);
					table.ForeignKey(
						name: "FK_OAuthConnections_Users_UserId",
						column: x => x.UserId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_OAuthConnections_UserId",
				table: "OAuthConnections",
				column: "UserId");

			migrationBuilder.CreateIndex(
				name: "IX_OAuthConnections_Provider_ExternalUserId",
				table: "OAuthConnections",
				columns: new[] { "Provider", "ExternalUserId" },
				unique: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropTable(
				name: "OAuthConnections");
		}
	}
}
