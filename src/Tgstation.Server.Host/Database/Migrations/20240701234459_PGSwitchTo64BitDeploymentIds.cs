using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <inheritdoc />
	public partial class PGSwitchTo64BitDeploymentIds : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AlterColumn<long>(
				name: "GitHubDeploymentId",
				table: "CompileJobs",
				type: "bigint",
				nullable: true,
				oldClrType: typeof(int),
				oldType: "integer",
				oldNullable: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AlterColumn<int>(
				name: "GitHubDeploymentId",
				table: "CompileJobs",
				type: "integer",
				nullable: true,
				oldClrType: typeof(long),
				oldType: "bigint",
				oldNullable: true);
		}
	}
}
