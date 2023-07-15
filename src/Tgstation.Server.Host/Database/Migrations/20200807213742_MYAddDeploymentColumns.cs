using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds columns for GitHub deployments for MYSQL.
	/// </summary>
	public partial class MYAddDeploymentColumns : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<bool>(
				name: "CreateGitHubDeployments",
				table: "RepositorySettings",
				nullable: false,
				defaultValue: false);

			migrationBuilder.AddColumn<int>(
				name: "GitHubDeploymentId",
				table: "CompileJobs",
				nullable: true);

			migrationBuilder.AddColumn<long>(
				name: "GitHubRepoId",
				table: "CompileJobs",
				nullable: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "CreateGitHubDeployments",
				table: "RepositorySettings");

			migrationBuilder.DropColumn(
				name: "GitHubDeploymentId",
				table: "CompileJobs");

			migrationBuilder.DropColumn(
				name: "GitHubRepoId",
				table: "CompileJobs");
		}
	}
}
