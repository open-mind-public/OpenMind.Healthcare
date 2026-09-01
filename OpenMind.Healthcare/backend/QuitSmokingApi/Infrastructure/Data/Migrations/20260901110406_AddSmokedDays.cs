using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuitSmokingApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSmokedDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SmokedDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuitJourneyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CigarettesSmoked = table.Column<int>(type: "INTEGER", nullable: false),
                    Trigger = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmokedDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmokedDays_QuitJourneys_QuitJourneyId",
                        column: x => x.QuitJourneyId,
                        principalTable: "QuitJourneys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SmokedDays_QuitJourneyId_Date",
                table: "SmokedDays",
                columns: new[] { "QuitJourneyId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SmokedDays");
        }
    }
}
