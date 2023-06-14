using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Renames the PullRequestRevision and adds the RepositoryOrigin columns for MSSQL.
	/// </summary>
	public partial class MSGenericTestMergingUpdate : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.RenameColumn(
				name: "PullRequestRevision",
				table: "TestMerges",
				newName: "TargetCommitSha");

			migrationBuilder.AddColumn<string>(
				name: "RepositoryOrigin",
				table: "CompileJobs",
				nullable: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.RenameColumn(
				name: "TargetCommitSha",
				table: "TestMerges",
				newName: "PullRequestRevision");

			migrationBuilder.DropColumn(
				name: "RepositoryOrigin",
				table: "CompileJobs");
		}
	}
}
