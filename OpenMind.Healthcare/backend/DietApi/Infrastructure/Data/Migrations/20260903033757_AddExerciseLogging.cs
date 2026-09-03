using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    SearchName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Met = table.Column<decimal>(type: "TEXT", precision: 4, scale: 1, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DietPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TotalMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalKilocalories = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseDays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExerciseDayId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActivityTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActivityName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Met = table.Column<decimal>(type: "TEXT", precision: 4, scale: 1, nullable: false),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedKcal = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseEntries_ExerciseDays_ExerciseDayId",
                        column: x => x.ExerciseDayId,
                        principalTable: "ExerciseDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTypes_SearchName",
                table: "ActivityTypes",
                column: "SearchName");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseDays_DietPlanId_Date",
                table: "ExerciseDays",
                columns: new[] { "DietPlanId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseDays_UserId_Date",
                table: "ExerciseDays",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseEntries_ExerciseDayId",
                table: "ExerciseEntries",
                column: "ExerciseDayId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityTypes");

            migrationBuilder.DropTable(
                name: "ExerciseEntries");

            migrationBuilder.DropTable(
                name: "ExerciseDays");
        }
    }
}
