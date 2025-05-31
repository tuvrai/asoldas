using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsOldAsApp.Migrations
{
    /// <inheritdoc />
    public partial class mssqllocal_migration_407 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    EntityId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DeathDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.EntityId);
                });

            migrationBuilder.CreateTable(
                name: "WikiEvent",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiEvent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonWikiEvent",
                columns: table => new
                {
                    PeopleEntityId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WikiEventId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonWikiEvent", x => new { x.PeopleEntityId, x.WikiEventId });
                    table.ForeignKey(
                        name: "FK_PersonWikiEvent_People_PeopleEntityId",
                        column: x => x.PeopleEntityId,
                        principalTable: "People",
                        principalColumn: "EntityId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonWikiEvent_WikiEvent_WikiEventId",
                        column: x => x.WikiEventId,
                        principalTable: "WikiEvent",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonWikiEvent_WikiEventId",
                table: "PersonWikiEvent",
                column: "WikiEventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonWikiEvent");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropTable(
                name: "WikiEvent");
        }
    }
}
