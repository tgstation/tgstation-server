using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds UserGroups for SQLite.
	/// </summary>
	public partial class SLAddUserGroups : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AddColumn<long>(
				name: "GroupId",
				table: "Users",
				nullable: true);

			migrationBuilder.CreateTable(
				name: "Groups",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Name = table.Column<string>(maxLength: 100, nullable: false),
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
						.Annotation("Sqlite:Autoincrement", true),
					AdministrationRights = table.Column<ulong>(nullable: false),
					InstanceManagerRights = table.Column<ulong>(nullable: false),
					UserId = table.Column<long>(nullable: true),
					GroupId = table.Column<long>(nullable: true),
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
						.Annotation("Sqlite:Autoincrement", true),
					PermissionSetId = table.Column<long>(nullable: false),
					InstancePermissionSetRights = table.Column<ulong>(nullable: false),
					ByondRights = table.Column<ulong>(nullable: false),
					DreamDaemonRights = table.Column<ulong>(nullable: false),
					DreamMakerRights = table.Column<ulong>(nullable: false),
					RepositoryRights = table.Column<ulong>(nullable: false),
					ChatBotRights = table.Column<ulong>(nullable: false),
					ConfigurationRights = table.Column<ulong>(nullable: false),
					InstanceId = table.Column<long>(nullable: false),
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

			migrationBuilder.RenameTable(
				name: "Users",
				newName: "Users_up");

			migrationBuilder.CreateTable(
				name: "Users",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Enabled = table.Column<bool>(nullable: false),
					CreatedAt = table.Column<DateTimeOffset>(nullable: false),
					SystemIdentifier = table.Column<string>(nullable: true),
					Name = table.Column<string>(maxLength: 10000, nullable: false),
					PasswordHash = table.Column<string>(nullable: true),
					CreatedById = table.Column<long>(nullable: true),
					GroupId = table.Column<long>(nullable: true),
					CanonicalName = table.Column<string>(nullable: false),
					LastPasswordUpdate = table.Column<DateTimeOffset>(nullable: true),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Users", x => x.Id);
					table.ForeignKey(
						name: "FK_Users_Users_CreatedById",
						column: x => x.CreatedById,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
					table.ForeignKey(
						name: "FK_Users_Groups_GroupId",
						column: x => x.GroupId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
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

			migrationBuilder.Sql(
				"INSERT INTO Users (Id, Enabled, CreatedAt, SystemIdentifier, Name, PasswordHash, CreatedById, CanonicalName, LastPasswordUpdate) SELECT Id, Enabled, CreatedAt, SystemIdentifier, Name, PasswordHash, CreatedById, CanonicalName, LastPasswordUpdate FROM Users_up");

			migrationBuilder.Sql(
				"INSERT INTO PermissionSets (UserId, AdministrationRights, InstanceManagerRights) SELECT Id, AdministrationRights, InstanceManagerRights FROM Users_up");

			migrationBuilder.Sql(
				"INSERT INTO InstancePermissionSets (PermissionSetId, InstanceId, InstancePermissionSetRights, ByondRights, DreamDaemonRights, DreamMakerRights, RepositoryRights, ChatBotRights, ConfigurationRights) SELECT p.Id, iu.InstanceId, iu.InstanceUserRights, iu.ByondRights, iu.DreamDaemonRights, iu.DreamMakerRights, iu.RepositoryRights, iu.ChatBotRights, iu.ConfigurationRights FROM InstanceUsers iu JOIN PermissionSets p ON iu.UserId = p.UserId");

			migrationBuilder.DropTable(
				name: "InstanceUsers");

			migrationBuilder.DropTable(
				name: "Users_up");

			// Had to do this in SLAllowNullDMApi too, renames confuse the fuck out of the ORM
			migrationBuilder.RenameTable(
				name: "Users",
				newName: "Users_up");

			migrationBuilder.RenameTable(
				name: "Users_up",
				newName: "Users");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.CreateTable(
				name: "InstanceUsers",
				columns: table => new
				{
					Id = table.Column<long>(type: "INTEGER", nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					ByondRights = table.Column<ulong>(type: "INTEGER", nullable: false),
					ChatBotRights = table.Column<ulong>(type: "INTEGER", nullable: false),
					ConfigurationRights = table.Column<ulong>(type: "INTEGER", nullable: false),
					DreamDaemonRights = table.Column<ulong>(type: "INTEGER", nullable: false),
					DreamMakerRights = table.Column<ulong>(type: "INTEGER", nullable: false),
					InstanceId = table.Column<long>(type: "INTEGER", nullable: false),
					InstanceUserRights = table.Column<ulong>(type: "INTEGER", nullable: false),
					RepositoryRights = table.Column<ulong>(type: "INTEGER", nullable: false),
					UserId = table.Column<long>(type: "INTEGER", nullable: false),
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

			migrationBuilder.RenameTable(
				name: "Users",
				newName: "Users_down");

			migrationBuilder.CreateTable(
				name: "Users",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Enabled = table.Column<bool>(nullable: false),
					CreatedAt = table.Column<DateTimeOffset>(nullable: false),
					SystemIdentifier = table.Column<string>(nullable: true),
					Name = table.Column<string>(maxLength: 100, nullable: false),
					AdministrationRights = table.Column<ulong>(nullable: false),
					InstanceManagerRights = table.Column<ulong>(nullable: false),
					PasswordHash = table.Column<string>(nullable: true),
					CreatedById = table.Column<long>(nullable: true),
					CanonicalName = table.Column<string>(maxLength: 100, nullable: false),
					LastPasswordUpdate = table.Column<DateTimeOffset>(nullable: true),
					GroupId = table.Column<long>(nullable: true),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Users", x => x.Id);
					table.ForeignKey(
						name: "FK_Users_Users_CreatedById",
						column: x => x.CreatedById,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
				});

			migrationBuilder.Sql(
				"INSERT INTO Users (Id, Enabled, CreatedAt, SystemIdentifier, Name, PasswordHash, CreatedById, CanonicalName, LastPasswordUpdate, AdministrationRights, InstanceManagerRights) SELECT u.Id, u.Enabled, u.CreatedAt, u.SystemIdentifier, u.Name, u.PasswordHash, u.CreatedById, u.CanonicalName, u.LastPasswordUpdate, p.AdministrationRights, p.InstanceManagerRights FROM Users_down u JOIN PermissionSets p ON p.UserId = u.Id WHERE u.GroupId = NULL");

			migrationBuilder.Sql(
				"INSERT INTO Users (Id, Enabled, CreatedAt, SystemIdentifier, Name, PasswordHash, CreatedById, CanonicalName, LastPasswordUpdate, AdministrationRights, InstanceManagerRights) SELECT u.Id, u.Enabled, u.CreatedAt, u.SystemIdentifier, u.Name, u.PasswordHash, u.CreatedById, u.CanonicalName, u.LastPasswordUpdate, p.AdministrationRights, p.InstanceManagerRights FROM Users_down u JOIN PermissionSets p ON p.GroupId = u.GroupId WHERE u.GroupId != NULL");

			migrationBuilder.Sql(
				"INSERT INTO InstanceUsers (UserId, InstanceId, InstanceUserRights, ByondRights, DreamDaemonRights, DreamMakerRights, RepositoryRights, ChatBotRights, ConfigurationRights) SELECT p.UserId, ips.InstanceId, ips.InstancePermissionSetRights, ips.ByondRights, ips.DreamDaemonRights, ips.DreamMakerRights, ips.RepositoryRights, ips.ChatBotRights, ips.ConfigurationRights FROM InstancePermissionSets ips JOIN PermissionSets p ON ips.PermissionSetId = p.Id WHERE p.UserId != NULL");

			migrationBuilder.Sql(
				"INSERT INTO InstanceUsers (UserId, InstanceId, InstanceUserRights, ByondRights, DreamDaemonRights, DreamMakerRights, RepositoryRights, ChatBotRights, ConfigurationRights) SELECT u.Id, ips.InstanceId, ips.InstancePermissionSetRights, ips.ByondRights, ips.DreamDaemonRights, ips.DreamMakerRights, ips.RepositoryRights, ips.ChatBotRights, ips.ConfigurationRights FROM InstancePermissionSets ips JOIN PermissionSets p ON ips.PermissionSetId = p.Id JOIN Users_down u ON p.GroupId = u.GroupId WHERE p.GroupId != NULL");

			migrationBuilder.DropTable(
				name: "InstancePermissionSets");

			migrationBuilder.DropTable(
				name: "PermissionSets");

			migrationBuilder.DropTable(
				name: "Groups");

			migrationBuilder.DropTable(
				name: "Users_down");

			migrationBuilder.RenameTable(
				name: "Users",
				newName: "Users_down");

			migrationBuilder.RenameTable(
				name: "Users_down",
				newName: "Users");
		}
	}
}
