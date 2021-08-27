using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Add DreamDaemon visibilty for SQLite.
	/// </summary>
	public partial class SLAddDreamDaemonVisibility : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AddColumn<int>(
				name: "LaunchVisibility",
				table: "ReattachInformations",
				nullable: false,
				defaultValue: 0);

			migrationBuilder.AddColumn<int>(
				name: "Visibility",
				table: "DreamDaemonSettings",
				nullable: false,
				defaultValue: 0);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.RenameTable(
				name: "ReattachInformations",
				newName: "ReattachInformations_down");

			migrationBuilder.CreateTable(
				name: "ReattachInformations",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					AccessIdentifier = table.Column<string>(nullable: false),
					ProcessId = table.Column<int>(nullable: false),
					Port = table.Column<ushort>(nullable: false),
					RebootState = table.Column<int>(nullable: false),
					LaunchSecurityLevel = table.Column<int>(nullable: false),
					CompileJobId = table.Column<long>(nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_ReattachInformations", x => x.Id);
					table.ForeignKey(
						name: "FK_ReattachInformations_CompileJobs_CompileJobId",
						column: x => x.CompileJobId,
						principalTable: "CompileJobs",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.Sql(
				$"INSERT INTO ReattachInformations SELECT Id,AccessIdentifier,ProcessId,Port,RebootState,LaunchSecurityLevel,CompileJobId FROM ReattachInformations_down");

			migrationBuilder.DropTable(
				name: "ReattachInformations_down");

			migrationBuilder.RenameTable(
				name: "ReattachInformations",
				newName: "ReattachInformations_down");

			migrationBuilder.RenameTable(
				name: "ReattachInformations_down",
				newName: "ReattachInformations");

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
				$"INSERT INTO DreamDaemonSettings SELECT Id,AllowWebClient,SecurityLevel,Port,AutoStart,HeartbeatSeconds,StartupTimeout,InstanceId,TopicRequestTimeout,AdditionalParameters FROM DreamDaemonSettings_down");

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
