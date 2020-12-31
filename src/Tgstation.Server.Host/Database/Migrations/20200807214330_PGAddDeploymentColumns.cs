using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds columns for GitHub deployments for PostgresSQL.
	/// </summary>
	public partial class PGAddDeploymentColumns : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

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
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

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
