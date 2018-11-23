using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Migrations.MySqlDatabase
{
	public partial class MYToggleTestmergeComments : Migration
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
