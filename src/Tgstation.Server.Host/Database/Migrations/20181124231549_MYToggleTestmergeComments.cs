using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Add the <see cref="Api.Models.RepositorySettings.PostTestMergeComment"/> column for MySQL/MariaDB.
	/// </summary>
	public partial class MYToggleTestmergeComments : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<bool>(
				name: "PostTestMergeComment",
				table: "RepositorySettings",
				nullable: false,
				defaultValue: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "PostTestMergeComment",
				table: "RepositorySettings");
		}
	}
}
