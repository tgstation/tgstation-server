using System;

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <inheritdoc />
	public partial class MYNormalizeVersionUpdates : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "Users",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "TestMerges",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "RevisionInformations",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "RevInfoTestMerges",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "RepositorySettings",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "ReattachInformations",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "PermissionSets",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "OAuthConnections",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "Jobs",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "Instances",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "InstancePermissionSets",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "Groups",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "DreamMakerSettings",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "DreamDaemonSettings",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "CompileJobs",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "ChatChannels",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

			migrationBuilder.AlterColumn<long>(
				name: "Id",
				table: "ChatBots",
				type: "bigint",
				nullable: false,
				oldClrType: typeof(long),
				oldType: "bigint")
				.Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
		}
	}
}
