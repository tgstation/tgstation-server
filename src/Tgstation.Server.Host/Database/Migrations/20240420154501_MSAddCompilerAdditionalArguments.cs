﻿using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <inheritdoc />
	public partial class MSAddCompilerAdditionalArguments : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.AddColumn<string>(
				name: "CompilerAdditionalArguments",
				table: "DreamMakerSettings",
				type: "nvarchar(max)",
				maxLength: 10000,
				nullable: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropColumn(
				name: "CompilerAdditionalArguments",
				table: "DreamMakerSettings");
		}
	}
}
