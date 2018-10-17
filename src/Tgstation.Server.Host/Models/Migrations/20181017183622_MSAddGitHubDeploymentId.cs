using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Models.Migrations
{
	/// <summary>
	/// Adds the <see cref="Octokit.Deployment.Id"/> column to <see cref="CompileJob"/>s for MSSQL
	/// </summary>
	public partial class MSAddGitHubDeploymentId : Migration
	{
		/// <summary>
		/// Applies the migration
		/// </summary>
		/// <param name="migrationBuilder">The <see cref="MigrationBuilder"/> to use</param>
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<int>(
				name: "GitHubDeploymentId",
				table: "CompileJobs",
				nullable: true);
		}

		/// <summary>
		/// Unapplies the migration
		/// </summary>
		/// <param name="migrationBuilder">The <see cref="MigrationBuilder"/> to use</param>
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "GitHubDeploymentId",
				table: "CompileJobs");
		}
	}
}
