using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds UserGroups for PostgresSql.
	/// </summary>
	public partial class PGAddUserGroups : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AlterColumn<string>(
				name: "SystemIdentifier",
				table: "Users",
				maxLength: 100,
				nullable: true,
				oldClrType: typeof(string),
				oldType: "text",
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "Name",
				table: "Users",
				maxLength: 100,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "character varying(10000)",
				oldMaxLength: 10000);

			migrationBuilder.AlterColumn<string>(
				name: "CanonicalName",
				table: "Users",
				maxLength: 100,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "text");

			migrationBuilder.AddColumn<long>(
				name: "GroupId",
				table: "Users",
				nullable: true);

			migrationBuilder.CreateTable(
				name: "Groups",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					Name = table.Column<string>(maxLength: 100, nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Groups", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "PermissionSets",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					AdministrationRights = table.Column<decimal>(nullable: false),
					InstanceManagerRights = table.Column<decimal>(nullable: false),
					UserId = table.Column<long>(nullable: true),
					GroupId = table.Column<long>(nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_PermissionSets", x => x.Id);
					table.ForeignKey(
						name: "FK_PermissionSets_Groups_GroupId",
						column: x => x.GroupId,
						principalTable: "Groups",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_PermissionSets_Users_UserId",
						column: x => x.UserId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "InstancePermissionSets",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					PermissionSetId = table.Column<long>(nullable: false),
					InstancePermissionSetRights = table.Column<decimal>(nullable: false),
					ByondRights = table.Column<decimal>(nullable: false),
					DreamDaemonRights = table.Column<decimal>(nullable: false),
					DreamMakerRights = table.Column<decimal>(nullable: false),
					RepositoryRights = table.Column<decimal>(nullable: false),
					ChatBotRights = table.Column<decimal>(nullable: false),
					ConfigurationRights = table.Column<decimal>(nullable: false),
					InstanceId = table.Column<long>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_InstancePermissionSets", x => x.Id);
					table.ForeignKey(
						name: "FK_InstancePermissionSets_Instances_InstanceId",
						column: x => x.InstanceId,
						principalTable: "Instances",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_InstancePermissionSets_PermissionSets_PermissionSetId",
						column: x => x.PermissionSetId,
						principalTable: "PermissionSets",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_Users_GroupId",
				table: "Users",
				column: "GroupId");

			migrationBuilder.CreateIndex(
				name: "IX_Groups_Name",
				table: "Groups",
				column: "Name",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_InstancePermissionSets_InstanceId",
				table: "InstancePermissionSets",
				column: "InstanceId");

			migrationBuilder.CreateIndex(
				name: "IX_InstancePermissionSets_PermissionSetId_InstanceId",
				table: "InstancePermissionSets",
				columns: new[] { "PermissionSetId", "InstanceId" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_PermissionSets_GroupId",
				table: "PermissionSets",
				column: "GroupId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_PermissionSets_UserId",
				table: "PermissionSets",
				column: "UserId",
				unique: true);

			migrationBuilder.AddForeignKey(
				name: "FK_Users_Groups_GroupId",
				table: "Users",
				column: "GroupId",
				principalTable: "Groups",
				principalColumn: "Id",
				onDelete: ReferentialAction.Restrict);

			migrationBuilder.Sql(
				"INSERT INTO \"PermissionSets\" (\"UserId\", \"AdministrationRights\", \"InstanceManagerRights\") SELECT \"Id\", \"AdministrationRights\", \"InstanceManagerRights\" FROM \"Users\"");

			migrationBuilder.Sql(
				"INSERT INTO  \"InstancePermissionSets\" (\"PermissionSetId\", \"InstanceId\", \"InstancePermissionSetRights\", \"ByondRights\", \"DreamDaemonRights\", \"DreamMakerRights\", \"RepositoryRights\", \"ChatBotRights\", \"ConfigurationRights\") SELECT p.\"Id\", iu.\"InstanceId\", iu.\"InstanceUserRights\", iu.\"ByondRights\", iu.\"DreamDaemonRights\", iu.\"DreamMakerRights\", iu.\"RepositoryRights\", iu.\"ChatBotRights\", iu.\"ConfigurationRights\" FROM \"InstanceUsers\" iu JOIN  \"PermissionSets\" p ON iu.\"UserId\" = p.\"UserId\"");

			migrationBuilder.DropTable(
				name: "InstanceUsers");

			migrationBuilder.DropColumn(
				name: "AdministrationRights",
				table: "Users");

			migrationBuilder.DropColumn(
				name: "InstanceManagerRights",
				table: "Users");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AlterColumn<string>(
				name: "SystemIdentifier",
				table: "Users",
				type: "text",
				nullable: true,
				oldClrType: typeof(string),
				oldMaxLength: 100,
				oldNullable: true);

			migrationBuilder.AlterColumn<string>(
				name: "Name",
				table: "Users",
				type: "character varying(10000)",
				maxLength: 10000,
				nullable: false,
				oldClrType: typeof(string),
				oldMaxLength: 100);

			migrationBuilder.AlterColumn<string>(
				name: "CanonicalName",
				table: "Users",
				type: "text",
				nullable: false,
				oldClrType: typeof(string),
				oldMaxLength: 100);

			migrationBuilder.AddColumn<decimal>(
				name: "AdministrationRights",
				table: "Users",
				type: "numeric(20,0)",
				nullable: false,
				defaultValue: 0m);

			migrationBuilder.AddColumn<decimal>(
				name: "InstanceManagerRights",
				table: "Users",
				type: "numeric(20,0)",
				nullable: false,
				defaultValue: 0m);

			migrationBuilder.CreateTable(
				name: "InstanceUsers",
				columns: table => new
				{
					Id = table.Column<long>(type: "bigint", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					ByondRights = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
					ChatBotRights = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
					ConfigurationRights = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
					DreamDaemonRights = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
					DreamMakerRights = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
					InstanceId = table.Column<long>(type: "bigint", nullable: false),
					InstanceUserRights = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
					RepositoryRights = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
					UserId = table.Column<long>(type: "bigint", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_InstanceUsers", x => x.Id);
					table.ForeignKey(
						name: "FK_InstanceUsers_Instances_InstanceId",
						column: x => x.InstanceId,
						principalTable: "Instances",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_InstanceUsers_Users_UserId",
						column: x => x.UserId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_InstanceUsers_InstanceId",
				table: "InstanceUsers",
				column: "InstanceId");

			migrationBuilder.CreateIndex(
				name: "IX_InstanceUsers_UserId_InstanceId",
				table: "InstanceUsers",
				columns: new[] { "UserId", "InstanceId" },
				unique: true);

			migrationBuilder.Sql(
				"UPDATE  \"Users\" SET \"AdministrationRights\" = p.\"AdministrationRights\", \"InstanceManagerRights\" = p.\"InstanceManagerRights\" FROM  \"Users\" u JOIN  \"PermissionSets\" p ON u.\"Id\" = p.\"UserId\" WHERE p.\"UserId\" != NULL");

			migrationBuilder.Sql(
				"UPDATE  \"Users\" SET \"AdministrationRights\" = p.\"AdministrationRights\", \"InstanceManagerRights\" = p.\"InstanceManagerRights\" FROM  \"Users\" u JOIN  \"PermissionSets\" p ON u.\"GroupId\" = p.\"GroupId\" WHERE p.\"GroupId\" != NULL");

			migrationBuilder.Sql(
				"INSERT INTO  \"InstanceUsers\" (\"UserId\", \"InstanceId\", \"InstanceUserRights\", \"ByondRights\", \"DreamDaemonRights\", \"DreamMakerRights\", \"RepositoryRights\", \"ChatBotRights\", \"ConfigurationRights\") SELECT p.\"UserId\", ips.\"InstanceId\", ips.\"InstancePermissionSetRights\", ips.\"ByondRights\", ips.\"DreamDaemonRights\", ips.\"DreamMakerRights\", ips.\"RepositoryRights\", ips.\"ChatBotRights\", ips.\"ConfigurationRights\" FROM  \"InstancePermissionSets\" ips JOIN  \"PermissionSets\" p ON ips.\"PermissionSetId\" = p.\"Id\" WHERE p.\"UserId\" != NULL");

			migrationBuilder.Sql(
				"INSERT INTO  \"InstanceUsers\" (\"UserId\", \"InstanceId\", \"InstanceUserRights\", \"ByondRights\", \"DreamDaemonRights\", \"DreamMakerRights\", \"RepositoryRights\", \"ChatBotRights\", \"ConfigurationRights\") SELECT u.\"Id\", ips.\"InstanceId\", ips.\"InstancePermissionSetRights\", ips.\"ByondRights\", ips.\"DreamDaemonRights\", ips.\"DreamMakerRights\", ips.\"RepositoryRights\", ips.\"ChatBotRights\", ips.\"ConfigurationRights\" FROM  \"InstancePermissionSets\" ips JOIN  \"PermissionSets\" p ON ips.\"PermissionSetId\" = p.\"Id\" JOIN \"Users\" u ON p.\"GroupId\" = u.\"GroupId\" WHERE p.\"GroupId\" != NULL");

			migrationBuilder.DropForeignKey(
				name: "FK_Users_Groups_GroupId",
				table: "Users");

			migrationBuilder.DropTable(
				name: "InstancePermissionSets");

			migrationBuilder.DropTable(
				name: "PermissionSets");

			migrationBuilder.DropTable(
				name: "Groups");

			migrationBuilder.DropIndex(
				name: "IX_Users_GroupId",
				table: "Users");

			migrationBuilder.DropColumn(
				name: "GroupId",
				table: "Users");
		}
	}
}
