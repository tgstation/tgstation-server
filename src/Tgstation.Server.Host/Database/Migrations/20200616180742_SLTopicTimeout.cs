using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Add BYOND topic timeouts for SQLite.
	/// </summary>
	public partial class SLTopicTimeout : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));
			migrationBuilder.AddColumn<uint>(
				name: "TopicRequestTimeout",
				table: "DreamDaemonSettings",
				nullable: false,
				defaultValue: 0u);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.RenameTable(
				name: "DreamDaemonSettings",
				newName: "DreamDaemonSettings_down");

			migrationBuilder.CreateTable(
				name: "DreamDaemonSettings",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					AllowWebClient = table.Column<bool>(nullable: false),
					SecurityLevel = table.Column<int>(nullable: false),
					PrimaryPort = table.Column<ushort>(nullable: false),
					SecondaryPort = table.Column<ushort>(nullable: false),
					StartupTimeout = table.Column<uint>(nullable: false),
					AutoStart = table.Column<bool>(nullable: false),
					InstanceId = table.Column<long>(nullable: false),
					HeartbeatSeconds = table.Column<uint>(nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_DreamDaemonSettings", x => x.Id);
					table.ForeignKey(
						name: "FK_DreamDaemonSettings_Instances_InstanceId",
						column: x => x.InstanceId,
						principalTable: "Instances",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.Sql(
				$"INSERT INTO DreamDaemonSettings SELECT Id,AllowWebClient,SecurityLevel,PrimaryPort,SecondaryPort,StartupTimeout,AutoStart,InstanceId,HeartbeatSeconds FROM DreamDaemonSettings_down");

			migrationBuilder.DropTable(
				name: "DreamDaemonSettings_down");
		}
	}
}
