using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the swarm identifier column for SQLite.
	/// </summary>
	public partial class SLAddSwarmIdentifer : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.DropIndex(
				name: "IX_Instances_Path",
				table: "Instances");

			migrationBuilder.AddColumn<string>(
				name: "SwarmIdentifer",
				table: "Instances",
				nullable: true);

			migrationBuilder.CreateIndex(
				name: "IX_Instances_Path_SwarmIdentifer",
				table: "Instances",
				columns: new[] { "Path", "SwarmIdentifer" },
				unique: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			ArgumentNullException.ThrowIfNull(migrationBuilder);

			migrationBuilder.RenameTable(
				name: "Instances",
				newName: "Instances_down");

			migrationBuilder.CreateTable(
				name: "Instances",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					Name = table.Column<string>(maxLength: 10000, nullable: false),
					Path = table.Column<string>(nullable: false),
					Online = table.Column<bool>(nullable: false),
					ConfigurationType = table.Column<int>(nullable: false),
					AutoUpdateInterval = table.Column<uint>(nullable: false),
					ChatBotLimit = table.Column<ushort>(nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Instances", x => x.Id);
				});

			migrationBuilder.Sql("INSERT INTO Instances (Id, Name, Path, Online, ConfigurationType, AutoUpdateInterval, ChatBotLimit) SELECT Id, Name, Path, Online, ConfigurationType, AutoUpdateInterval, ChatBotLimit FROM Instances_down");

			migrationBuilder.DropTable(
				name: "Instances_down");

			migrationBuilder.RenameTable(
				name: "Instances",
				newName: "Instances_down");

			migrationBuilder.RenameTable(
				name: "Instances_down",
				newName: "Instances");
		}
	}
}
