using Microsoft.EntityFrameworkCore.Migrations;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Models.Migrations
{
	/// <summary>
	/// Add the <see cref="Api.Models.Internal.CompileJob.MinimumSecurityLevel"/> and <see cref="DreamMaker.ApiValidationSecurityLevel"/> columns for MSSQL
	/// </summary>
	public partial class MSAddMinimumSecurity : Migration
	{
		/// <summary>
		/// Applies the migration
		/// </summary>
		/// <param name="migrationBuilder">The <see cref="MigrationBuilder"/> to use</param>
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<int>(
				name: "ApiValidationSecurityLevel",
				table: "DreamMakerSettings",
				nullable: false,
				defaultValue: (int)DreamDaemonSecurity.Safe);

			migrationBuilder.AddColumn<int>(
				name: "MinimumSecurityLevel",
				table: "CompileJobs",
				nullable: false,
				defaultValue: (int)DreamDaemonSecurity.Safe);
		}

		/// <summary>
		/// Unapplies the migration
		/// </summary>
		/// <param name="migrationBuilder">The <see cref="MigrationBuilder"/> to use</param>
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "ApiValidationSecurityLevel",
				table: "DreamMakerSettings");

			migrationBuilder.DropColumn(
				name: "MinimumSecurityLevel",
				table: "CompileJobs");
		}
	}
}
