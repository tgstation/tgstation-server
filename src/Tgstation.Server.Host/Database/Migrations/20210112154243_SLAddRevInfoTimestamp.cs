using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Tgstation.Server.Host.Database.Migrations
{
	/// <summary>
	/// Add the Timestamp column to RevisionInformations for SQLite.
	/// </summary>
	public partial class SLAddRevInfoTimestamp : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.AddColumn<DateTimeOffset>(
				name: "Timestamp",
				table: "RevisionInformations",
				nullable: false,
				defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			if (migrationBuilder == null)
				throw new ArgumentNullException(nameof(migrationBuilder));

			migrationBuilder.RenameTable(
				name: "RevisionInformations",
				newName: "RevisionInformations_down");

			migrationBuilder.CreateTable(
				name: "RevisionInformations",
				columns: table => new
				{
					Id = table.Column<long>(nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					CommitSha = table.Column<string>(maxLength: 40, nullable: false),
					OriginCommitSha = table.Column<string>(maxLength: 40, nullable: false),
					InstanceId = table.Column<long>(nullable: false),
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_RevisionInformations", x => x.Id);
					table.ForeignKey(
						name: "FK_RevisionInformations_Instances_InstanceId",
						column: x => x.InstanceId,
						principalTable: "Instances",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.Sql("INSERT INTO RevisionInformations (Id, CommitSha, OriginCommitSha, InstanceId) SELECT Id, CommitSha, OriginCommitSha, InstanceId FROM RevisionInformations_down");

			migrationBuilder.DropTable(
				name: "RevisionInformations_down");

			migrationBuilder.RenameTable(
				name: "RevisionInformations",
				newName: "RevisionInformations_down");

			migrationBuilder.RenameTable(
				name: "RevisionInformations_down",
				newName: "RevisionInformations");
		}
	}
}
