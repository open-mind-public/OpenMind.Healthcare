using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DietPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Goal = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    HeightCm = table.Column<decimal>(type: "TEXT", precision: 5, scale: 1, nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Sex = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ActivityLevel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TargetCalories = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetProteinG = table.Column<decimal>(type: "TEXT", precision: 6, scale: 1, nullable: true),
                    TargetCarbsG = table.Column<decimal>(type: "TEXT", precision: 6, scale: 1, nullable: true),
                    TargetFatG = table.Column<decimal>(type: "TEXT", precision: 6, scale: 1, nullable: true),
                    TargetSource = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TargetWeightKg = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DietPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeightReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DietPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    WeightKg = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeightReadings_DietPlans_DietPlanId",
                        column: x => x.DietPlanId,
                        principalTable: "DietPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DietPlans_UserId",
                table: "DietPlans",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeightReadings_DietPlanId_Date",
                table: "WeightReadings",
                columns: new[] { "DietPlanId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeightReadings");

            migrationBuilder.DropTable(
                name: "DietPlans");
        }
    }
}
