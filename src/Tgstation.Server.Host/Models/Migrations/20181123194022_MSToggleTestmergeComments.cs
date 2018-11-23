using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Migrations.SqlServerDatabase
{
	public partial class MSToggleTestmergeComments : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<bool>(
				name: "RepositorySettings",
				table: "PostTestMergeComment",
				nullable: false,
				defaultValue: false);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "RepositorySettings",
				table: "PostTestMergeComment");
		}
	}
}
