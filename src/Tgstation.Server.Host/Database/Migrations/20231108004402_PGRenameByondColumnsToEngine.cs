using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <inheritdoc />
	public partial class PGRenameByondColumnsToEngine : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.RenameColumn(
				name: "ByondRights",
				table: "InstancePermissionSets",
				newName: "EngineRights");

			migrationBuilder.RenameColumn(
				name: "ByondVersion",
				table: "CompileJobs",
				newName: "EngineVersion");

			migrationBuilder.AlterColumn<decimal>(
				name: "InstanceManagerRights",
				table: "PermissionSets",
				type: "numeric(20,0)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20)");

			migrationBuilder.AlterColumn<decimal>(
				name: "AdministrationRights",
				table: "PermissionSets",
				type: "numeric(20,0)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20)");

			migrationBuilder.AlterColumn<decimal>(
				name: "CancelRightsType",
				table: "Jobs",
				type: "numeric(20,0)",
				nullable: true,
				oldClrType: typeof(decimal),
				oldType: "numeric(20)",
				oldNullable: true);

			migrationBuilder.AlterColumn<decimal>(
				name: "CancelRight",
				table: "Jobs",
				type: "numeric(20,0)",
				nullable: true,
				oldClrType: typeof(decimal),
				oldType: "numeric(20)",
				oldNullable: true);

			migrationBuilder.AlterColumn<decimal>(
				name: "RepositoryRights",
				table: "InstancePermissionSets",
				type: "numeric(20,0)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20)");

			migrationBuilder.AlterColumn<decimal>(
				name: "InstancePermissionSetRights",
				table: "InstancePermissionSets",
				type: "numeric(20,0)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20)");

			migrationBuilder.AlterColumn<decimal>(
				name: "DreamMakerRights",
				table: "InstancePermissionSets",
				type: "numeric(20,0)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20)");

			migrationBuilder.AlterColumn<decimal>(
				name: "DreamDaemonRights",
				table: "InstancePermissionSets",
				type: "numeric(20,0)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20)");

			migrationBuilder.AlterColumn<decimal>(
				name: "ConfigurationRights",
				table: "InstancePermissionSets",
				type: "numeric(20,0)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20)");

			migrationBuilder.AlterColumn<decimal>(
				name: "ChatBotRights",
				table: "InstancePermissionSets",
				type: "numeric(20,0)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20)");

			migrationBuilder.AlterColumn<decimal>(
				name: "EngineRights",
				table: "InstancePermissionSets",
				type: "numeric(20,0)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20)");

			migrationBuilder.AlterColumn<decimal>(
				name: "DiscordChannelId",
				table: "ChatChannels",
				type: "numeric(20,0)",
				nullable: true,
				oldClrType: typeof(decimal),
				oldType: "numeric(20)",
				oldNullable: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.RenameColumn(
				name: "EngineRights",
				table: "InstancePermissionSets",
				newName: "ByondRights");

			migrationBuilder.RenameColumn(
				name: "EngineVersion",
				table: "CompileJobs",
				newName: "ByondVersion");

			migrationBuilder.AlterColumn<decimal>(
				name: "InstanceManagerRights",
				table: "PermissionSets",
				type: "numeric(20)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20,0)");

			migrationBuilder.AlterColumn<decimal>(
				name: "AdministrationRights",
				table: "PermissionSets",
				type: "numeric(20)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20,0)");

			migrationBuilder.AlterColumn<decimal>(
				name: "CancelRightsType",
				table: "Jobs",
				type: "numeric(20)",
				nullable: true,
				oldClrType: typeof(decimal),
				oldType: "numeric(20,0)",
				oldNullable: true);

			migrationBuilder.AlterColumn<decimal>(
				name: "CancelRight",
				table: "Jobs",
				type: "numeric(20)",
				nullable: true,
				oldClrType: typeof(decimal),
				oldType: "numeric(20,0)",
				oldNullable: true);

			migrationBuilder.AlterColumn<decimal>(
				name: "RepositoryRights",
				table: "InstancePermissionSets",
				type: "numeric(20)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20,0)");

			migrationBuilder.AlterColumn<decimal>(
				name: "InstancePermissionSetRights",
				table: "InstancePermissionSets",
				type: "numeric(20)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20,0)");

			migrationBuilder.AlterColumn<decimal>(
				name: "DreamMakerRights",
				table: "InstancePermissionSets",
				type: "numeric(20)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20,0)");

			migrationBuilder.AlterColumn<decimal>(
				name: "DreamDaemonRights",
				table: "InstancePermissionSets",
				type: "numeric(20)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20,0)");

			migrationBuilder.AlterColumn<decimal>(
				name: "ConfigurationRights",
				table: "InstancePermissionSets",
				type: "numeric(20)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20,0)");

			migrationBuilder.AlterColumn<decimal>(
				name: "ChatBotRights",
				table: "InstancePermissionSets",
				type: "numeric(20)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20,0)");

			migrationBuilder.AlterColumn<decimal>(
				name: "ByondRights",
				table: "InstancePermissionSets",
				type: "numeric(20)",
				nullable: false,
				oldClrType: typeof(decimal),
				oldType: "numeric(20,0)");

			migrationBuilder.AlterColumn<decimal>(
				name: "DiscordChannelId",
				table: "ChatChannels",
				type: "numeric(20)",
				nullable: true,
				oldClrType: typeof(decimal),
				oldType: "numeric(20,0)",
				oldNullable: true);
		}
	}
}
