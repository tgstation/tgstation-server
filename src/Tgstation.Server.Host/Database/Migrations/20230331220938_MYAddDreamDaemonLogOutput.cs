using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the DreamDaemon LogOutput column for MYSQL. Also corrects string column annotations.
	/// </summary>
	public partial class MYAddDreamDaemonLogOutput : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AlterColumn<string>(
				name: "Comment",
				table: "TestMerges",
				type: "varchar(10000)",
				maxLength: 10000,
				nullable: true,
				oldClrType: typeof(string),
				oldType: "longtext CHARACTER SET utf8mb4",
				oldMaxLength: 10000,
				oldNullable: true)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "CommitterName",
				table: "RepositorySettings",
				type: "varchar(10000)",
				maxLength: 10000,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "longtext CHARACTER SET utf8mb4",
				oldMaxLength: 10000)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "CommitterEmail",
				table: "RepositorySettings",
				type: "varchar(10000)",
				maxLength: 10000,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "longtext CHARACTER SET utf8mb4",
				oldMaxLength: 10000)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "AccessUser",
				table: "RepositorySettings",
				type: "varchar(10000)",
				maxLength: 10000,
				nullable: true,
				oldClrType: typeof(string),
				oldType: "longtext CHARACTER SET utf8mb4",
				oldMaxLength: 10000,
				oldNullable: true)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "AccessToken",
				table: "RepositorySettings",
				type: "varchar(10000)",
				maxLength: 10000,
				nullable: true,
				oldClrType: typeof(string),
				oldType: "longtext CHARACTER SET utf8mb4",
				oldMaxLength: 10000,
				oldNullable: true)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "ProjectName",
				table: "DreamMakerSettings",
				type: "varchar(10000)",
				maxLength: 10000,
				nullable: true,
				oldClrType: typeof(string),
				oldType: "longtext CHARACTER SET utf8mb4",
				oldMaxLength: 10000,
				oldNullable: true)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "AdditionalParameters",
				table: "DreamDaemonSettings",
				type: "varchar(10000)",
				maxLength: 10000,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "longtext CHARACTER SET utf8mb4",
				oldMaxLength: 10000)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AddColumn<bool>(
				name: "LogOutput",
				table: "DreamDaemonSettings",
				type: "tinyint(1)",
				nullable: false,
				defaultValue: false);

			migrationBuilder.AlterColumn<string>(
				name: "Tag",
				table: "ChatChannels",
				type: "varchar(10000)",
				maxLength: 10000,
				nullable: true,
				oldClrType: typeof(string),
				oldType: "longtext CHARACTER SET utf8mb4",
				oldMaxLength: 10000,
				oldNullable: true)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "ConnectionString",
				table: "ChatBots",
				type: "varchar(10000)",
				maxLength: 10000,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "longtext CHARACTER SET utf8mb4",
				oldMaxLength: 10000)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropColumn(
				name: "LogOutput",
				table: "DreamDaemonSettings");

			migrationBuilder.AlterColumn<string>(
				name: "PasswordHash",
				table: "Users",
				type: "longtext CHARACTER SET utf8mb4",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "longtext",
				oldNullable: true)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "Url",
				table: "TestMerges",
				type: "longtext CHARACTER SET utf8mb4",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "longtext")
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "TitleAtMerge",
				table: "TestMerges",
				type: "longtext CHARACTER SET utf8mb4",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "longtext")
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "Comment",
				table: "TestMerges",
				type: "longtext CHARACTER SET utf8mb4",
				maxLength: 10000,
				nullable: true,
				oldClrType: typeof(string),
				oldType: "varchar(10000)",
				oldMaxLength: 10000,
				oldNullable: true)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "BodyAtMerge",
				table: "TestMerges",
				type: "longtext CHARACTER SET utf8mb4",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "longtext")
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "Author",
				table: "TestMerges",
				type: "longtext CHARACTER SET utf8mb4",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "longtext")
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "CommitterName",
				table: "RepositorySettings",
				type: "longtext CHARACTER SET utf8mb4",
				maxLength: 10000,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "varchar(10000)",
				oldMaxLength: 10000)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "CommitterEmail",
				table: "RepositorySettings",
				type: "longtext CHARACTER SET utf8mb4",
				maxLength: 10000,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "varchar(10000)",
				oldMaxLength: 10000)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "AccessUser",
				table: "RepositorySettings",
				type: "longtext CHARACTER SET utf8mb4",
				maxLength: 10000,
				nullable: true,
				oldClrType: typeof(string),
				oldType: "varchar(10000)",
				oldMaxLength: 10000,
				oldNullable: true)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "AccessToken",
				table: "RepositorySettings",
				type: "longtext CHARACTER SET utf8mb4",
				maxLength: 10000,
				nullable: true,
				oldClrType: typeof(string),
				oldType: "varchar(10000)",
				oldMaxLength: 10000,
				oldNullable: true)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "AccessIdentifier",
				table: "ReattachInformations",
				type: "longtext CHARACTER SET utf8mb4",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "longtext")
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "ExceptionDetails",
				table: "Jobs",
				type: "longtext CHARACTER SET utf8mb4",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "longtext",
				oldNullable: true)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "Description",
				table: "Jobs",
				type: "longtext CHARACTER SET utf8mb4",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "longtext")
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "ProjectName",
				table: "DreamMakerSettings",
				type: "longtext CHARACTER SET utf8mb4",
				maxLength: 10000,
				nullable: true,
				oldClrType: typeof(string),
				oldType: "varchar(10000)",
				oldMaxLength: 10000,
				oldNullable: true)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "AdditionalParameters",
				table: "DreamDaemonSettings",
				type: "longtext CHARACTER SET utf8mb4",
				maxLength: 10000,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "varchar(10000)",
				oldMaxLength: 10000)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "RepositoryOrigin",
				table: "CompileJobs",
				type: "longtext CHARACTER SET utf8mb4",
				nullable: true,
				oldClrType: typeof(string),
				oldType: "longtext",
				oldNullable: true)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "Output",
				table: "CompileJobs",
				type: "longtext CHARACTER SET utf8mb4",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "longtext")
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "DmeName",
				table: "CompileJobs",
				type: "longtext CHARACTER SET utf8mb4",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "longtext")
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "ByondVersion",
				table: "CompileJobs",
				type: "longtext CHARACTER SET utf8mb4",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "longtext")
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "Tag",
				table: "ChatChannels",
				type: "longtext CHARACTER SET utf8mb4",
				maxLength: 10000,
				nullable: true,
				oldClrType: typeof(string),
				oldType: "varchar(10000)",
				oldMaxLength: 10000,
				oldNullable: true)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");

			migrationBuilder.AlterColumn<string>(
				name: "ConnectionString",
				table: "ChatBots",
				type: "longtext CHARACTER SET utf8mb4",
				maxLength: 10000,
				nullable: false,
				oldClrType: typeof(string),
				oldType: "varchar(10000)",
				oldMaxLength: 10000)
				.Annotation("MySql:CharSet", "utf8mb4")
				.OldAnnotation("MySql:CharSet", "utf8mb4");
		}
	}
}
