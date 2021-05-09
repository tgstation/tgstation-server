using System;

using Microsoft.EntityFrameworkCore.Migrations;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Add the <see cref="Api.Models.Internal.CompileJob.MinimumSecurityLevel"/> and <see cref="Api.Models.Internal.DreamMakerSettings.ApiValidationSecurityLevel"/> columns for MSSQL.
	/// </summary>
	public partial class MSAddMinimumSecurity : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

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

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.DropColumn(
				name: "ApiValidationSecurityLevel",
				table: "DreamMakerSettings");

			migrationBuilder.DropColumn(
				name: "MinimumSecurityLevel",
				table: "CompileJobs");
		}
	}
}
