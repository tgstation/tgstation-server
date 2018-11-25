using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Models.Migrations
{
	/// <summary>
	/// Add the <see cref="Api.Models.Internal.RepositorySettings.PostTestMergeComment"/> column for MSSQL
	/// </summary>
	public partial class MSToggleTestmergeComments : Migration
	{
		/// <summary>
		/// Applies the migration
		/// </summary>
		/// <param name="migrationBuilder">The <see cref="MigrationBuilder"/> to use</param>
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<bool>(
				name: "PostTestMergeComment",
				table: "RepositorySettings",
				nullable: false,
				defaultValue: true);
		}

		/// <summary>
		/// Unapplies the migration
		/// </summary>
		/// <param name="migrationBuilder">The <see cref="MigrationBuilder"/> to use</param>
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "PostTestMergeComment",
				table: "RepositorySettings");
		}
	}
}
