using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Adds the option to start the profiler with DreamDaemon for SQLite.
	/// </summary>
	public partial class SLAddProfiler : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AddColumn<bool>(
				name: "StartProfiler",
				table: "DreamDaemonSettings",
				nullable: false,
				defaultValue: false);
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
					Port = table.Column<ushort>(nullable: false),
					StartupTimeout = table.Column<uint>(nullable: false),
					HeartbeatSeconds = table.Column<uint>(nullable: false),
					AutoStart = table.Column<bool>(nullable: false),
					InstanceId = table.Column<long>(nullable: false),
					TopicRequestTimeout = table.Column<uint>(nullable: false),
					AdditionalParameters = table.Column<string>(maxLength: 10000, nullable: false),
					Visibility = table.Column<bool>(nullable: false),
					DumpOnHeartbeatRestart = table.Column<bool>(nullable: false),
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
				$"INSERT INTO DreamDaemonSettings SELECT Id,AllowWebClient,SecurityLevel,Port,AutoStart,HeartbeatSeconds,StartupTimeout,InstanceId,TopicRequestTimeout,AdditionalParameters,Visibility,DumpOnHeartbeatRestart FROM DreamDaemonSettings_down");

			migrationBuilder.DropTable(
				name: "DreamDaemonSettings_down");

			migrationBuilder.RenameTable(
				name: "DreamDaemonSettings",
				newName: "DreamDaemonSettings_down");

			migrationBuilder.RenameTable(
				name: "DreamDaemonSettings_down",
				newName: "DreamDaemonSettings");
		}
	}
}
