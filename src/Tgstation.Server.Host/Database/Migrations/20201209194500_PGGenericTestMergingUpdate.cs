using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Renames the PullRequestRevision and adds the RepositoryOrigin columns for PostgresSQL.
	/// </summary>
	public partial class PGGenericTestMergingUpdate : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

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
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

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
